using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// External controllers optionally used
using PhysicsBasedCharacterController; // CharacterManager events (optional)
using Lightbug.CharacterControllerPro.Core; // CharacterActor (velocity/grounded)
using Lightbug.CharacterControllerPro.Demo; // Dash (optional)

/// <summary>
/// Drop this on Base.prefab, assign the dust effect prefabs.
/// It auto-wires to movement/dash/jump/land/hit using existing scripts
/// (PlayerReference/CharacterActor, PlayerDash, CharacterManager, Dash demo) when available.
/// </summary>
[DisallowMultipleComponent]
public class PlayerDustFX : MonoBehaviour
{
    [Header("References (auto-detected if empty)")]
    public Transform feet;                      // Optional feet reference; if null, computed from collider bottom
    public PlayerReference playerReference;     // Auto-found on Start

    [Header("Prefabs / Effects (drag & drop)")]
    public GameObject walkOrRunFX;              // 1. Walk or Run (small step puffs)
    public GameObject dashFX;                   // 2. Dash
    public GameObject jumpTiltedFX;             // 3. Jump Tilted
    public GameObject jumpStraightFX;           // 4. Jump Straight
    public GameObject breakFX;                  // 5. Break (sudden stop skid)
    public GameObject puffFX;                   // 6. Puff (generic small burst)
    public GameObject hitFX;                    // 7. Hit (on damage)
    public GameObject jumpLandFallFX;           // 8. Jump or Landing or Falling (generic)
    public GameObject groundImpactFX;           // 9. Ground Impact (hard landing)
    public GameObject windBlownFX;              // 10. Wind blown dust (continuous at high speed)

    [Header("Tuning")]
    public LayerMask groundMask = ~0;
    public float minWalkSpeed = 0.5f;
    public float stepInterval = 0.25f;
    public float breakDecelThreshold = 8f;          // m/s instantaneous decel to trigger break
    public float landingImpactSpeed = 7f;           // y-speed magnitude to trigger groundImpactFX
    public float fallStartSpeed = 2f;               // start falling FX when going down faster than this
    public float windSpeedThreshold = 8f;           // activate wind blown dust over this horizontal speed
    public float fxLifetimeFallback = 5f;           // Destroy after N seconds if prefab has no self-destroy

    [Header("Spawn Offsets/Options")]
    public Vector3 feetOffset = new Vector3(0, 0.05f, 0);
    public bool alignToGroundNormal = true;

    [Header("Variants")]
    public bool alternateRunWithBreak = true;       // Alternate step FX between run and break to add variation
    public bool randomizeRunBreak = false;          // If true, random pick instead of strict alternation

    // Cached components
    private CharacterActor actor;                       // From Lightbug CCP
    private Rigidbody rb;                               // Fallback
    private CapsuleCollider capsule;
    private CharacterManager physxCharacterManager;     // Optional alt controller
    private Dash demoDash;                              // Optional demo dash (Lightbug)
    private PlayerDash customDash;                      // Custom dash controller

    // State
    private float stepTimer;
    private bool lastGrounded;
    private Vector3 lastVelocity;
    private bool lastDashing;
    private float lastHealth = -1f;
    private bool fallingFXActive;
    private GameObject windFXInstance;
    private bool stepToggle;

    void Awake()
    {
        if (!playerReference) playerReference = GetComponent<PlayerReference>();
        actor = playerReference ? playerReference.characterActor : GetComponent<CharacterActor>();
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        physxCharacterManager = GetComponent<CharacterManager>();
        demoDash = GetComponentInChildren<Dash>();
        customDash = GetComponent<PlayerDash>();
    }

    void OnEnable()
    {
        // Subscribe to optional events
        if (physxCharacterManager != null)
        {
            try
            {
                // Subscribe via reflection-safe pattern in case events are null
                var jumpEvt = typeof(CharacterManager).GetField("OnJump", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var landEvt = typeof(CharacterManager).GetField("OnLand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var fastEvt = typeof(CharacterManager).GetField("OnFast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var sprintEvt = typeof(CharacterManager).GetField("OnSprint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var crouchEvt = typeof(CharacterManager).GetField("OnCrouch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;

                if (jumpEvt != null) jumpEvt.AddListener(OnJumpEvent);
                if (landEvt != null) landEvt.AddListener(OnLandEvent);
                if (fastEvt != null) fastEvt.AddListener(OnFastEvent);
                if (sprintEvt != null) sprintEvt.AddListener(OnSprintEvent);
                if (crouchEvt != null) crouchEvt.AddListener(OnCrouchEvent);
            }
            catch { /* ignore if API changes */ }
        }

        if (actor != null)
        {
            actor.OnGroundedStateEnter += OnActorGroundedEnter;
            actor.OnGroundedStateExit += OnActorGroundedExit;
        }

        if (demoDash != null)
        {
            demoDash.OnDashStart += OnDash;
            demoDash.OnDashEnd += _ => lastDashing = false;
        }

        if (customDash != null)
        {
            customDash.OnDashStartEvent += OnCustomDashStart;
            customDash.OnDashEndEvent += OnCustomDashEnd;
        }

        if (playerReference != null && playerReference.playerStatistics != null)
        {
            playerReference.playerStatistics.OnLocalHealthChanged += OnLocalHealthChanged;
        }
    }

    void OnDisable()
    {
        if (physxCharacterManager != null)
        {
            try
            {
                var jumpEvt = typeof(CharacterManager).GetField("OnJump", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var landEvt = typeof(CharacterManager).GetField("OnLand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var fastEvt = typeof(CharacterManager).GetField("OnFast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var sprintEvt = typeof(CharacterManager).GetField("OnSprint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;
                var crouchEvt = typeof(CharacterManager).GetField("OnCrouch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(physxCharacterManager) as UnityEngine.Events.UnityEvent;

                if (jumpEvt != null) jumpEvt.RemoveListener(OnJumpEvent);
                if (landEvt != null) landEvt.RemoveListener(OnLandEvent);
                if (fastEvt != null) fastEvt.RemoveListener(OnFastEvent);
                if (sprintEvt != null) sprintEvt.RemoveListener(OnSprintEvent);
                if (crouchEvt != null) crouchEvt.RemoveListener(OnCrouchEvent);
            }
            catch { }
        }

        if (actor != null)
        {
            actor.OnGroundedStateEnter -= OnActorGroundedEnter;
            actor.OnGroundedStateExit -= OnActorGroundedExit;
        }

        if (demoDash != null)
        {
            demoDash.OnDashStart -= OnDash;
            demoDash.OnDashEnd -= _ => lastDashing = false;
        }

        if (customDash != null)
        {
            customDash.OnDashStartEvent -= OnCustomDashStart;
            customDash.OnDashEndEvent -= OnCustomDashEnd;
        }

        if (playerReference != null && playerReference.playerStatistics != null)
        {
            playerReference.playerStatistics.OnLocalHealthChanged -= OnLocalHealthChanged;
        }
    }

    void Start()
    {
        if (feet == null)
        {
            if (capsule != null)
                feet = new GameObject(name + "_FeetRef").transform;
            else
                feet = this.transform;
        }
    }

    void Update()
    {
#if UNITY_SERVER
        return; // VFX local only
#endif
        bool grounded = GetGrounded();
        Vector3 velocity = GetVelocity();
        Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);

        // Maintain feet reference at bottom of collider (if available)
        if (capsule != null && feet != null)
        {
            Vector3 bottom = transform.position + Vector3.down * (capsule.height * 0.5f - capsule.radius);
            feet.position = bottom + feetOffset;
        }

        // 1. Walk/Run steps (follow player while alive)
        if (grounded && horizontal.magnitude >= minWalkSpeed)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                GameObject stepFx = walkOrRunFX;
                if (alternateRunWithBreak && breakFX != null)
                {
                    if (randomizeRunBreak)
                    {
                        stepFx = UnityEngine.Random.value < 0.5f ? walkOrRunFX : breakFX;
                    }
                    else
                    {
                        stepToggle = !stepToggle;
                        stepFx = stepToggle ? breakFX : walkOrRunFX;
                    }
                }
                SpawnAtFeetFollow(stepFx);
            }
        }
        else
        {
            stepTimer = Mathf.Min(stepTimer, stepInterval * 0.5f);
        }

        // 3/4/8/9. Jump/Fall/Land/Impact handled by CharacterActor events when available.
        // Fallback only if actor is missing
        if (actor == null)
        {
            if (!lastGrounded && grounded)
            {
                SpawnAtFeet(jumpLandFallFX);
                if (Mathf.Abs(lastVelocity.y) > landingImpactSpeed) SpawnAtFeet(groundImpactFX);
                fallingFXActive = false;
            }
            else if (lastGrounded && !grounded && velocity.y > 0.1f)
            {
                if (horizontal.magnitude > 1.5f) SpawnAtFeet(jumpTiltedFX);
                else SpawnAtFeet(jumpStraightFX);
            }
            else if (!grounded && velocity.y < -fallStartSpeed && !fallingFXActive)
            {
                SpawnAtFeet(jumpLandFallFX);
                fallingFXActive = true;
            }
        }

        // 5. Break (sudden deceleration while grounded)
        float decel = (lastVelocity - velocity).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        if (grounded && decel > breakDecelThreshold && horizontal.magnitude < minWalkSpeed)
        {
            SpawnAtFeet(breakFX);
        }

        // 7. Hit handled by PlayerStatistics event; fallback polling if not available
        if (playerReference == null || playerReference.playerStatistics == null)
        {
            TryHitFX();
        }

        // 10. Wind blown dust (toggle by speed)
        HandleWindFX(horizontal.magnitude);

        lastGrounded = grounded;
        lastVelocity = velocity;
    }

    private void TryHitFX()
    {
        if (playerReference == null || playerReference.playerStatistics == null) return;
        var stats = playerReference.playerStatistics.playerStatData;
        if (lastHealth < 0f)
        {
            lastHealth = stats.health;
            return;
        }
        if (stats.health < lastHealth - 0.1f)
        {
            SpawnAroundBody(hitFX);
        }
        lastHealth = stats.health;
    }

    private void HandleWindFX(float horizontalSpeed)
    {
        bool shouldPlay = horizontalSpeed >= windSpeedThreshold && GetGrounded();
        if (shouldPlay)
        {
            if (windFXInstance == null)
            {
                windFXInstance = InstantiateSafe(windBlownFX, GetFeetSpawn(out Vector3 pos, out Quaternion rot) ? pos : transform.position, Quaternion.identity);
                if (windFXInstance != null)
                    windFXInstance.transform.SetParent(transform);
            }
            else
            {
                var ps = windFXInstance.GetComponentInChildren<ParticleSystem>();
                if (ps != null && !ps.isPlaying) ps.Play();
            }
        }
        else
        {
            if (windFXInstance != null)
            {
                var ps = windFXInstance.GetComponentInChildren<ParticleSystem>();
                if (ps != null) ps.Stop();
            }
        }
    }

    // Event handlers (CharacterManager)
    private void OnJumpEvent() { /* Jump start handled in Update edge */ }
    private void OnLandEvent() { /* Land handled in Update edge */ }
    private void OnFastEvent() { /* Steps handled continuously */ }
    private void OnSprintEvent() { SpawnAtFeet(puffFX); }
    private void OnCrouchEvent() { SpawnAtFeet(puffFX); }

    // Event handlers (Dash)
    private void OnDash(Vector3 dir)
    {
        lastDashing = true;
        SpawnAtFeet(dashFX);
    }

    private void OnCustomDashStart()
    {
        lastDashing = true;
        SpawnAtFeet(dashFX);
    }

    private void OnCustomDashEnd()
    {
        lastDashing = false;
    }

    // CharacterActor grounded events
    private void OnActorGroundedEnter(Vector3 localVelocity)
    {
        SpawnAtFeet(jumpLandFallFX);
        if (Mathf.Abs(lastVelocity.y) > landingImpactSpeed) SpawnAtFeet(groundImpactFX);
        fallingFXActive = false;
    }

    private void OnActorGroundedExit()
    {
        Vector3 v = GetVelocity();
        Vector3 h = new Vector3(v.x, 0, v.z);
        if (v.y > 0.1f)
        {
            if (h.magnitude > 1.5f) SpawnAtFeet(jumpTiltedFX);
            else SpawnAtFeet(jumpStraightFX);
        }
        else
        {
            if (!fallingFXActive)
            {
                SpawnAtFeet(jumpLandFallFX);
                fallingFXActive = true;
            }
        }
    }

    // Health change event
    private void OnLocalHealthChanged(float prev, float curr)
    {
        if (curr < prev - 0.1f) SpawnAroundBody(hitFX);
    }

    // Public API for manual triggers (Animator events etc.)
    public void TriggerPuff() => SpawnAtFeet(puffFX);
    public void TriggerBreak() => SpawnAtFeet(breakFX);
    public void TriggerStep() => SpawnAtFeet(walkOrRunFX);

    // Helpers
    private bool GetGrounded()
    {
        if (actor != null) return actor.IsGrounded;
        if (playerReference && playerReference.characterActor != null) return playerReference.characterActor.IsGrounded;
        if (capsule != null)
        {
            Vector3 origin = transform.position + Vector3.down * (capsule.height * 0.5f - capsule.radius + 0.01f);
            return Physics.CheckSphere(origin, capsule.radius * 0.9f, groundMask, QueryTriggerInteraction.Ignore);
        }
        return Physics.Raycast(transform.position, Vector3.down, 0.2f, groundMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetVelocity()
    {
        if (actor != null) return actor.Velocity;
        if (playerReference && playerReference.characterActor != null) return playerReference.characterActor.Velocity;
        if (rb != null) return rb.velocity;
        return Vector3.zero;
    }

    private void SpawnAtFeet(GameObject prefab)
    {
        if (prefab == null) return;
        if (!GetFeetSpawn(out Vector3 position, out Quaternion rotation))
        {
            position = (feet ? feet.position : transform.position) + feetOffset;
            rotation = Quaternion.identity;
        }
        InstantiateAndCleanup(prefab, position, rotation);
    }

    private void SpawnAtFeetFollow(GameObject prefab)
    {
        if (prefab == null) return;
        if (!GetFeetSpawn(out Vector3 position, out Quaternion rotation))
        {
            position = (feet ? feet.position : transform.position) + feetOffset;
            rotation = Quaternion.identity;
        }
        var go = InstantiateAndCleanup(prefab, position, rotation);
        if (go != null)
        {
            // Parent to player to follow transform during its lifetime
            go.transform.SetParent(this.transform, true);
        }
    }

    private void SpawnAroundBody(GameObject prefab)
    {
        if (prefab == null) return;
        Vector3 position = transform.position + Vector3.up * 1f;
        Quaternion rotation = Quaternion.identity;
        InstantiateAndCleanup(prefab, position, rotation);
    }

    private bool GetFeetSpawn(out Vector3 position, out Quaternion rotation)
    {
        Vector3 start = (feet ? feet.position : transform.position) + Vector3.up * 0.2f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 1.0f, groundMask, QueryTriggerInteraction.Ignore))
        {
            position = hit.point + feetOffset;
            rotation = alignToGroundNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity;
            return true;
        }
        position = Vector3.zero; rotation = Quaternion.identity; return false;
    }

    private GameObject InstantiateAndCleanup(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var go = InstantiateSafe(prefab, position, rotation);
        if (go == null) return null;
        // If the prefab has a particle system, destroy after its duration; otherwise fallback
        float ttl = fxLifetimeFallback;
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            if (main.loop)
                ttl = Mathf.Max(fxLifetimeFallback, 3f);
            else
                ttl = Mathf.Max(main.duration + main.startLifetime.constantMax, 1f);
        }
        Destroy(go, ttl);
        return go;
    }

    private GameObject InstantiateSafe(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        try
        {
            return Instantiate(prefab, position, rotation);
        }
        catch
        {
            return null;
        }
    }
}

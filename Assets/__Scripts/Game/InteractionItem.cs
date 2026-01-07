using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractionItem : NetworkBehaviour
{
    public bool given;
    public UnityEvent onTriggerEnter;

    public float delay = -1f;

    [Header("Auto collect")]
    [SerializeField, Tooltip("Give the pickup automatically to the player stored inside DeserveFor.")]
    private bool autoCollectForDeservePlayer = false;
    [SerializeField, Tooltip("When auto collect is enabled, move the pickup toward the player instead of granting instantly.")]
    private bool useHomingForAutoCollect = false;
    [SerializeField, Tooltip("If true, only the player stored inside DeserveFor can take the pickup.")]
    private bool restrictPickupToDeservePlayer = false;

    [Header("Homing settings")]
    [SerializeField] private Vector3 homingOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float homingStartSpeed = 5f;
    [SerializeField] private float homingAcceleration = 25f;
    [SerializeField] private float homingMaxSpeed = 20f;
    [SerializeField] private float homingArriveDistance = 0.65f;

    private PlayerReference _deserveFor;
    private Collider _collider;
    private bool _homingActive;
    private float _currentHomingSpeed;
    private bool _startInvoked;
    private NetworkObjectReference _pendingDeserveForRef;
    private bool _pendingClientAssignment;

    public PlayerReference DeserveFor => _deserveFor;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null && !_collider.isTrigger)
        {
            _collider.isTrigger = true;
        }
    }

    private void Start()
    {
        _startInvoked = true;
#if UNITY_SERVER
        if (IsServer && delay > 0f)
        {
            Destroy(gameObject, delay);
        }
#endif
        TryStartAutoCollect();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && _pendingClientAssignment)
        {
            // We might have assigned a target before the item was spawned; forward it to clients now.
            AssignDeserveForClientRpc(_pendingDeserveForRef);
            _pendingClientAssignment = false;
        }
    }

    private void Update()
    {
        if (!_homingActive) return;
        if (_deserveFor == null)
        {
            StopHoming();
            return;
        }

        Vector3 targetPos = _deserveFor.transform.position + homingOffset;
        Vector3 toTarget = targetPos - transform.position;

        if (toTarget.sqrMagnitude <= homingArriveDistance * homingArriveDistance)
        {
            if (!TryGiveTo(_deserveFor))
            {
                StopHoming();
            }
            return;
        }

        Vector3 direction = toTarget.normalized;
        float maxSpeed = homingMaxSpeed <= 0f ? float.MaxValue : homingMaxSpeed;
        float startSpeed = Mathf.Max(0f, homingStartSpeed);
        _currentHomingSpeed = Mathf.Clamp(_currentHomingSpeed + homingAcceleration * Time.deltaTime, startSpeed, maxSpeed);

        transform.position += direction * _currentHomingSpeed * Time.deltaTime;
    }

    private bool HasServerAuthority()
    {
        return NetworkManager.Singleton == null || IsServer;
    }

    public void AssignDeserveFor(PlayerReference playerRef, bool triggerAutoCollect = true)
    {
        Debug.Log(playerRef);
        if (playerRef == null || playerRef.NetworkObject == null)
        {
            return;
        }

        _deserveFor = playerRef;

        if (triggerAutoCollect)
        {
            TryStartAutoCollect();
        }


        var reference = (NetworkObjectReference)playerRef.NetworkObject;
        if (!IsSpawned)
        {
            // Remember the target so we can notify clients once this item spawns.
            _pendingDeserveForRef = reference;
            _pendingClientAssignment = true;
            return;
        }

        AssignDeserveForClientRpc(reference);
    }

    [ClientRpc]
    private void AssignDeserveForClientRpc(NetworkObjectReference playerRef)
    {
        Debug.Log("AssignDeserveForClientRpc");
        if (IsServer) return;
        Debug.Log("AssignDeserveForClientRpc");

        PlayerReference resolved = null;
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            resolved = netObj.GetComponent<PlayerReference>();
        }

        _deserveFor = resolved;
        TryStartAutoCollect();
    }

    private void TryStartAutoCollect()
    {
        if (!_startInvoked) return;
        if (!autoCollectForDeservePlayer) return;
        if (_deserveFor == null) return;
        if (_homingActive || given) return;

        if (useHomingForAutoCollect)
        {
            BeginHoming();
        }
        else if (HasServerAuthority())
        {
            TryGiveTo(_deserveFor);
        }
    }

    private void BeginHoming()
    {
        _homingActive = true;
        _currentHomingSpeed = Mathf.Max(_currentHomingSpeed, homingStartSpeed);
    }

    private void StopHoming()
    {
        _homingActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 3 && other.gameObject.layer != 9) return;
        if (given) return;

        var playerRef = other.GetComponent<PlayerReference>();
        if (playerRef == null) return;

        if (restrictPickupToDeservePlayer && _deserveFor != null && playerRef != _deserveFor)
            return;

        TryGiveTo(playerRef);
    }

    private bool TryGiveTo(PlayerReference playerRef)
    {
        if (playerRef == null) return false;
        if (given) return false;

        var statistics = playerRef.playerStatistics;
        if (statistics == null) return false;

        var stats = statistics.playerStatData;
        if (stats.health <= 0f) return false;

        given = true;
        StopHoming();
        onTriggerEnter?.Invoke();

#if UNITY_SERVER
        ApplyServerRewards(playerRef, statistics, stats);
#endif

        return true;
    }

#if UNITY_SERVER
    private void ApplyServerRewards(PlayerReference playerRef, PlayerStatistics statistics, StatStruct stats)
    {
        int prevSoul = stats.soul;
        stats.soul = Mathf.Min(stats.soul + 1, 50);

        bool isBattleRoyale = Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale;
        if (isBattleRoyale)
        {
            if (prevSoul < 50)
            {
                stats.maxHealth += 5f;
            }
            stats.health += 8f;
        }
        else
        {

            int healStacks = 0;
            int maxStacks = 0;


            stats.health += healStacks * HealPerStack;
            stats.maxHealth += maxStacks * MaxHealthPerStack;
        }

        if (stats.health > stats.maxHealth) stats.health = stats.maxHealth;

        statistics.playerStatData = stats;
        statistics.UpdateSoulHealthClientRpc(stats);

        Destroy(gameObject, 1.25f);
    }

    private const int HealBonusId = 1;
    private const int MaxHealthBonusId = 2;
    private const float HealPerStack = 1f;
    private const float MaxHealthPerStack = 0.1f;
#endif
}

using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Lightbug.CharacterControllerPro.Core;

public class DashBots : NetworkBehaviour
{
    private PlayerReference playerReference;
    private CharacterController controller;
    private PlayerClasses animator;

    public bool debugAutoDash = true;


    public AnimationCurve movementCurve = AnimationCurve.Linear(0, 1, 1, 0);

    public bool forceNotGrounded = true;
    public bool cancelOnContact = true;

    private Vector3 dashDirection = Vector3.forward;
    private float dashCursor = 0f;
    private Follow follow;
    public float dashSpeed = 12f;
    public float dashTime = 0.4f;
    public float dashCD = 0f;
    public float jumpForce = 8f;


    private bool isDashing;
    public bool IsDashing => isDashing;

    public event System.Action OnDashStartEvent;
    public event System.Action OnDashEndEvent;

public bool DashAwayFromTarget(Transform target)
{
    if (target == null)
        return false;

    if (!CanDash())
        return false;

    Vector3 dir = transform.position - target.position;
    dir.y = 0f;

    if (dir.sqrMagnitude < 0.01f)
        dir = -transform.forward;

    dashDirection = dir.normalized;

    StartCoroutine(Dash());
    return true;
}

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<PlayerClasses>();
        playerReference = GetComponent<PlayerReference>();
        follow = GetComponent<Follow>();
    }

    /*private void Update()
    {
        if (!IsServer) return;

        // Cooldown
        if (dashCD > 0f)
        {
            dashCD -= Time.deltaTime;
            if (dashCD < 0f)
                dashCD = 0f;
        }
        // IA simple
        if (ShouldDash())
        {
            DashAction();
        }
    }*/

    private void Update()
    {
        if (!IsServer) return;

        if (dashCD > 0f)
        {
            dashCD -= Time.deltaTime;
            if (dashCD < 0f)
                dashCD = 0f;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Test dash manuel");
            DashAction();
        }
    }


    private bool ShouldDash()
    {
        if (isDashing || dashCD > 0f)
            return false;

        if (playerReference == null || playerReference.characterBrain == null)
            return false;

        return playerReference.characterBrain.characterActions.movement.value.sqrMagnitude > 0.01f;
    }

    private IEnumerator Dash()
    {
        if (playerReference == null || animator == null || animator.animator == null)
            yield break;

        var brain = playerReference.characterBrain;
        var actor = playerReference.characterActor;

        if (brain == null || actor == null)
            yield break;

        isDashing = true;

        bool previousRootMotion = actor.UseRootMotion;

        try { OnDashStartEvent?.Invoke(); } catch { }

        brain.characterActions.dash.value = true;

        // Animation
        if (IsServer)
        {
            animator.animator.Play("roulade", 0);
            animator.animator.Play("roulade", 1);
            PlayAnimationClientRpc(NetworkObject, "roulade");
        }

        SoundManager.Instance.Play2D("dash");

        // Désancrage
        bool resetAlwaysNotGrounded = false;
        if (forceNotGrounded && !actor.alwaysNotGrounded)
        {
            actor.alwaysNotGrounded = true;
            resetAlwaysNotGrounded = true;
        }

        actor.UseRootMotion = false;
        dashCursor = 0f;

        float elapsed = 0f;

        while (elapsed < dashTime)
        {
            if (cancelOnContact && actor.WallContacts.Count != 0)
                break;

            float factor = movementCurve.Evaluate(dashCursor);
            Vector3 velocity = dashSpeed * factor * dashDirection;

            actor.Velocity = velocity;

            dashCursor += Time.deltaTime / dashTime;
            elapsed += Time.deltaTime;

            yield return null;
        }

        // Fin du dash
        actor.Velocity = Vector3.zero;

        if (resetAlwaysNotGrounded)
            actor.alwaysNotGrounded = false;

        actor.UseRootMotion = previousRootMotion;

        isDashing = false;

        try { OnDashEndEvent?.Invoke(); } catch { }

        brain.characterActions.dash.value = false;
        dashCD = 3f;
    }
    private bool CanDash()
    {
        if (isDashing)
            return false;

        if (playerReference == null || playerReference.playerStatistics == null || playerReference.characterBrain == null)
            return false;

        if (playerReference.playerMovement != null && playerReference.playerMovement.IsDashBlocked())
            return false;

        if (playerReference.playerStatistics.StunAirSeconds > 0f ||
            playerReference.playerStatistics.StunSeconds > 0f ||
            playerReference.playerStatistics.FreezeSeconds > 0f ||
            playerReference.playerStatistics.SleepSeconds > 0f ||
            playerReference.playerStatistics.ParaSeconds > 0f)
            return false;

        if (playerReference.playerStatistics.ServerBlockSeconds > 0f)
            return false;

        if (playerReference.playerStatistics.playerStatData.health <= 0f)
            return false;

        if (playerReference.playerStatistics.isBehindWho)
            return false;

        if (playerReference.characterBrain.inputHandlerSettings != null &&
            playerReference.characterBrain.inputHandlerSettings.InputHandler != null &&
            playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting)
            return false;

        if (animator == null || animator.animator == null || !animator.animator.isInitialized)
            return false;

        if (animator.animator.GetBool("spellPriority"))
            return false;

        if (dashCD > 0f)
            return false;

        return true;
    }

    /*public bool DashAction()
    {
        if (!CanDash())
            return false;

        var brain = playerReference.characterBrain;
        var actor = playerReference.characterActor;

        if (brain == null || actor == null)
            return false;

        if (brain.characterActions.movement.value.sqrMagnitude > 0.01f)
        {
            dashDirection = (brain.characterActions.movement.value.y * actor.Forward +
                            brain.characterActions.movement.value.x * actor.Right).normalized;
        }
        else
        {
            dashDirection = actor.Forward;
        }

        StartCoroutine(Dash());
        return true;
    }*/

    public bool DashAction()
{
    Debug.Log("DashAction appelée");

    if (!CanDash())
    {
        Debug.Log("DashAction refusée par CanDash()");
        return false;
    }

    var brain = playerReference.characterBrain;
    var actor = playerReference.characterActor;

    if (brain == null || actor == null)
    {
        Debug.Log("DashAction annulée : brain ou actor null");
        return false;
    }

    if (brain.characterActions.movement.value.sqrMagnitude > 0.01f)
    {
        dashDirection = (brain.characterActions.movement.value.y * actor.Forward +
                        brain.characterActions.movement.value.x * actor.Right).normalized;
    }
    else
    {
        dashDirection = actor.Forward;
    }

    Debug.Log("DashAction acceptée, direction = " + dashDirection);
    StartCoroutine(Dash());
    return true;
}

    [ClientRpc]
    public void PlayAnimationClientRpc(NetworkObjectReference casterObject, string animationName)
    {
        if (casterObject.TryGet(out NetworkObject casterNet))
        {
            GameObject caster = casterNet.gameObject;
            var playerClasses = caster.GetComponent<PlayerClasses>();

            if (playerClasses != null && playerClasses.animator != null)
            {
                playerClasses.animator.Play(animationName, 0);
                playerClasses.animator.Play(animationName, 1);
            }
        }
    }
}
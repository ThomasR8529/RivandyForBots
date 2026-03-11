using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

/// <summary>
/// Système d'esquive complet pour les monstres.
/// S'appuie sur Follow + PhysicsMonster + CombatMonster.
/// À ajouter sur le même GameObject que Follow.
/// </summary>
[RequireComponent(typeof(Follow))]
[RequireComponent(typeof(PhysicsMonster))]
public class DodgeMonster : NetworkBehaviour
{
    // ─────────────────────────────────────────────
    //  Références
    // ─────────────────────────────────────────────
    private Follow follow;
    private PhysicsMonster physicsMonster;
    private Animator animator;

    // ─────────────────────────────────────────────
    //  Paramètres — Recul (doBackward amélioré)
    // ─────────────────────────────────────────────
    [Header("=== Recul amélioré ===")]
    [Tooltip("Vitesse maximale du recul")]
    [SerializeField] private float retreatSpeed = 4f;

    [Tooltip("Courbe d'accélération du recul (0→1 en X, vitesse relative en Y)")]
    [SerializeField] private AnimationCurve retreatCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Légère variation angulaire aléatoire du recul (degrés)")]
    [SerializeField, Range(0f, 45f)] private float retreatAngleVariance = 15f;

    [Tooltip("Durée du recul en secondes")]
    [SerializeField] private float retreatDuration = 0.5f;

    // ─────────────────────────────────────────────
    //  Paramètres — Esquive latérale
    // ─────────────────────────────────────────────
    [Header("=== Esquive latérale ===")]
    [Tooltip("Distance du pas latéral")]
    [SerializeField] private float lateralDodgeDistance = 2.5f;

    [Tooltip("Cooldown entre deux esquives latérales")]
    [SerializeField] private float lateralDodgeCooldown = 2.5f;

    [Tooltip("Probabilité (0-1) que l'esquive latérale soit choisie à la place du recul")]
    [SerializeField, Range(0f, 1f)] private float lateralDodgeChance = 0.45f;

    // ─────────────────────────────────────────────
    //  Paramètres — Dash d'esquive
    // ─────────────────────────────────────────────
    [Header("=== Dash d'esquive ===")]
    [Tooltip("Active le dash d'esquive")]
    [SerializeField] private bool enableDash = true;

    [Tooltip("Distance du dash")]
    [SerializeField] private float dashDistance = 4f;

    [Tooltip("Durée du dash")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("Cooldown du dash")]
    [SerializeField] private float dashCooldown = 5f;

    [Tooltip("Nom du trigger Animator pour le dash (laisser vide pour ignorer)")]
    [SerializeField] private string dashAnimTrigger = "dodge";

    // ─────────────────────────────────────────────
    //  Paramètres — Esquive prédictive
    // ─────────────────────────────────────────────
    [Header("=== Esquive prédictive ===")]
    [Tooltip("Active la détection de projectiles entrants")]
    [SerializeField] private bool enablePredictive = true;

    [Tooltip("Rayon de détection des projectiles (sphere autour du monstre)")]
    [SerializeField] private float projectileDetectionRadius = 4f;

    [Tooltip("Layer des projectiles ennemis")]
    [SerializeField] private LayerMask projectileLayer = 1 << 8;   // ajustez selon votre projet

    [Tooltip("Fréquence de scan (secondes entre deux vérifications)")]
    [SerializeField] private float predictiveScanInterval = 0.15f;

    [Tooltip("Vitesse minimale d'un projectile pour déclencher l'esquive")]
    [SerializeField] private float minProjectileSpeed = 3f;

    // ─────────────────────────────────────────────
    //  État interne
    // ─────────────────────────────────────────────
    private float lastLateralDodgeTime = -99f;
    private float lastDashTime        = -99f;
    private bool  isDodging           = false;

    // ─────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────
    private void Awake()
    {
        follow        = GetComponent<Follow>();
        physicsMonster = GetComponent<PhysicsMonster>();
    }

    public override void OnNetworkSpawn()
    {
        // Récupération de l'animator une fois le réseau prêt
        if (follow.monsterReference != null && follow.monsterReference.playerClasses != null)
            animator = follow.monsterReference.playerClasses.animator;

        if (enablePredictive && IsServer)
            StartCoroutine(PredictiveScanRoutine());
    }

    public void TriggerSmartRetreat()
    {
        if (!IsServer || isDodging || follow.IsMovementBlocked() || physicsMonster.IsPushing)
            return;

        // Priorité 1 : dash si disponible et monstre assez proche du danger
        if (enableDash && CanDash())
        {
            StartCoroutine(DashDodgeRoutine());
            return;
        }

        // Priorité 2 : esquive latérale (aléatoire)
        if (CanLateralDodge() && Random.value < lateralDodgeChance)
        {
            StartCoroutine(LateralDodgeRoutine());
            return;
        }

        // Priorité 3 : recul fluide amélioré
        StartCoroutine(SmoothRetreatRoutine());
    }

    // ─────────────────────────────────────────────
    //  Recul fluide amélioré
    // ─────────────────────────────────────────────
    private IEnumerator SmoothRetreatRoutine()
    {
        isDodging = true;

        // Direction de base : s'éloigner de la cible
        Vector3 awayDir = GetAwayDirection();

        // Variation angulaire aléatoire pour un mouvement moins robotique
        float angle = Random.Range(-retreatAngleVariance, retreatAngleVariance);
        awayDir = Quaternion.Euler(0f, angle, 0f) * awayDir;

        float elapsed = 0f;

        while (elapsed < retreatDuration)
        {
            if (follow.IsMovementBlocked() || physicsMonster.IsPushing)
                break;

            float t          = elapsed / retreatDuration;
            float speedMult  = retreatCurve.Evaluate(t);
            Vector3 targetPos = follow.agent.transform.position + awayDir * retreatSpeed * speedMult * Time.deltaTime;

            // Vérification NavMesh avant de bouger
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                follow.agent.SetDestination(hit.position);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
    }

    // ─────────────────────────────────────────────
    //  Esquive latérale
    // ─────────────────────────────────────────────
    private bool CanLateralDodge() =>
        Time.time >= lastLateralDodgeTime + lateralDodgeCooldown;

    private IEnumerator LateralDodgeRoutine()
    {
        isDodging            = true;
        lastLateralDodgeTime = Time.time;

        Vector3 dodgeDir = ChooseLateralDirection();
        Vector3 targetPos = follow.agent.transform.position + dodgeDir * lateralDodgeDistance;

        // Snap sur le NavMesh
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, lateralDodgeDistance, NavMesh.AllAreas))
        {
            follow.agent.speed = follow.originalStoppingDistance * 2f; // sprint latéral
            follow.agent.SetDestination(hit.position);

            // Attendre l'arrivée ou timeout
            float timeout = 1.5f;
            float elapsed = 0f;
            while (elapsed < timeout && follow.agent.remainingDistance > 0.3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isDodging = false;
    }

    /// <summary>Choisit gauche ou droite selon l'espace disponible sur le NavMesh.</summary>
    private Vector3 ChooseLateralDirection()
    {
        Vector3 toTarget = follow.TargetXform != null
            ? (follow.TargetXform.position - follow.agent.transform.position).normalized
            : follow.agent.transform.forward;
        toTarget.y = 0f;

        Vector3 left  = Vector3.Cross(Vector3.up,  toTarget).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, -toTarget).normalized;

        bool leftOk  = NavMesh.SamplePosition(follow.agent.transform.position + left  * lateralDodgeDistance, out _, 1.2f, NavMesh.AllAreas);
        bool rightOk = NavMesh.SamplePosition(follow.agent.transform.position + right * lateralDodgeDistance, out _, 1.2f, NavMesh.AllAreas);

        if  (leftOk && rightOk) return Random.value > 0.5f ? left : right;
        if  (leftOk)            return left;
        if  (rightOk)           return right;
        return left; // fallback
    }

    // ─────────────────────────────────────────────
    //  Dash d'esquive
    // ─────────────────────────────────────────────
    private bool CanDash() =>
        Time.time >= lastDashTime + dashCooldown;

    private IEnumerator DashDodgeRoutine()
    {
        isDodging   = true;
        lastDashTime = Time.time;

        // Direction : s'éloigner + légère composante latérale aléatoire
        Vector3 awayDir   = GetAwayDirection();
        Vector3 lateralDir = ChooseLateralDirection();
        Vector3 dashDir    = (awayDir * 0.6f + lateralDir * 0.4f).normalized;

        Vector3 dashTarget = follow.agent.transform.position + dashDir * dashDistance;

        // Trigger animation
        if (animator != null && !string.IsNullOrEmpty(dashAnimTrigger))
            TriggerDashAnimClientRpc();

        // Désactivation brève du NavMeshAgent → déplacement direct
        follow.agent.enabled = false;

        float elapsed = 0f;
        Vector3 startPos = follow.agent.transform.position;

        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            // Courbe ease-out pour le dash
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            follow.agent.transform.position = Vector3.Lerp(startPos, dashTarget, ease);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ré-ancrage sur le NavMesh
        if (NavMesh.SamplePosition(follow.agent.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            follow.agent.transform.position = hit.position;

        follow.agent.enabled = true;

        if (!follow.agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallback, 6f, NavMesh.AllAreas))
                follow.agent.Warp(fallback.position);
        }

        isDodging = false;
    }

    [ClientRpc]
    private void TriggerDashAnimClientRpc()
    {
        if (animator != null && !string.IsNullOrEmpty(dashAnimTrigger))
            animator.SetTrigger(dashAnimTrigger);
    }

    // ─────────────────────────────────────────────
    //  Esquive prédictive (scan projectiles)
    // ─────────────────────────────────────────────
    private IEnumerator PredictiveScanRoutine()
    {
        var wait = new WaitForSeconds(predictiveScanInterval);
        while (true)
        {
            yield return wait;

            if (follow.IsMovementBlocked() || physicsMonster.IsPushing || isDodging)
                continue;

            Collider[] hits = Physics.OverlapSphere(
                follow.agent.transform.position,
                projectileDetectionRadius,
                projectileLayer
            );

            foreach (var col in hits)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb == null) continue;
                if (rb.velocity.magnitude < minProjectileSpeed) continue;

                // Vérifie que le projectile se dirige vers le monstre
                Vector3 toMonster = follow.agent.transform.position - col.transform.position;
                float dot = Vector3.Dot(rb.velocity.normalized, toMonster.normalized);

                if (dot > 0.6f) // projectile en direction du monstre
                {
                    TriggerSmartRetreat();
                    break; // un seul déclenchement par scan
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Utilitaires
    // ─────────────────────────────────────────────
    private Vector3 GetAwayDirection()
    {
        if (follow.TargetXform == null)
            return -follow.agent.transform.forward;

        Vector3 dir = follow.agent.transform.position - follow.TargetXform.position;
        dir.y = 0f;
        return dir == Vector3.zero ? -follow.agent.transform.forward : dir.normalized;
    }

    /// <summary>Expose l'état pour Follow.cs (ex. bloquer les attaques pendant l'esquive).</summary>
    public bool IsDodging => isDodging;

    // Gizmo de debug en éditeur
    private void OnDrawGizmosSelected()
    {
        if (!enablePredictive) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, projectileDetectionRadius);
    }
}
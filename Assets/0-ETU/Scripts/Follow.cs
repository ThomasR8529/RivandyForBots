using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using SurvivorMode;
using PhysicsBasedCharacterController;
using System.Linq;
using Org.BouncyCastle.Bcpg;
using Unity.Netcode.Components;


[RequireComponent(typeof(CombatMonster))]
[RequireComponent(typeof(PhysicsMonster))]

public class Follow : NetworkBehaviour
{
    public CombatMonster CombatMonster;
    public PhysicsMonster physicsMonster;

    // === Champs généraux ===
    [SerializeField] public NavMeshAgent agent;
    [HideInInspector] public DodgeMonster dodgeMonster;

    private Animator animator;
    public PlayerReference monsterReference;

    public bool isDead;
    [Header("Cibleur: le transform qui gÃ¨re le lancer pour tirer le projectile")]
    public Transform cibleur;
    [Header("doBackward: est-ce-que le monstre doit reculer s'il est au corps Ã  corps ? ")]
    public bool doBackward;

    [SerializeField] private LayerMask targetableLayers = (1 << 9) | (1 << 3) | (1 << 5);
    public PlayerReference cible;
    [HideInInspector] public Transform defendTarget;
    public Transform TargetXform => (cible != null ? cible.transform : defendTarget);

    [HideInInspector, SerializeField] public Vector3 defaultPosition;
    [SerializeField] public float rotationSpeed = 6f;

    private Vector3 previousPosition;
    private Coroutine activateFaceBack;
    private float stationaryTimer = 0f;
    private float targetEvaluationTimer = 0f;

    public Billboard billboardEntity;
    public string twitchName;

    public float originalStoppingDistance;
    private float originalSpeed;

    public List<List<float>> movementSpeedFactors;

    private bool isRetreat;
    private float stunAccumulated;

    public NavMeshAgent GetAgent() => agent;

    private void Awake()
    {
        CombatMonster = GetComponent<CombatMonster>();
        physicsMonster = GetComponent<PhysicsMonster>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("On NETWORK SPAWN");
        originalStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = 0;
        monsterReference = agent.GetComponent<PlayerReference>();
        animator = monsterReference.playerClasses.animator;
        monsterReference.playerStatistics.playerInstanciated = true;
        monsterReference.follow = this;
        defaultPosition = gameObject.transform.position;
        movementSpeedFactors = new List<List<float>>();

        if (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer)
        {
            int wave = Run.instance.CPUcontroller.zone.currentWave;
            var stats = monsterReference.playerStatistics.playerStatData;
            stats.maxHealth = Mathf.Max(1f, stats.maxHealth);
            stats.health = Mathf.Max(1f, stats.health);
            monsterReference.playerStatistics.playerStatData = stats;
            // Keep existing speed behavior
            agent.speed += agent.speed * (Run.instance.CPUcontroller.zone.currentWave * 0.1f);
            agent.speed = agent.speed > 16 ? 16 : agent.speed;
            originalSpeed = agent.speed;
        }
        else
        {
            agent.speed = 1;
            originalSpeed = agent.speed;
        }

        if (SurvivorModifiers.defendStoneHeartThisWave && StoneHeartTarget.Instance != null)
        {
            defendTarget = StoneHeartTarget.Instance.transform;
        }
        else
        {
            defendTarget = null;
        }
        dodgeMonster = GetComponent<DodgeMonster>();

#if UNITY_SERVER
		previousPosition = agent.transform.position;
		StartCoroutine(ResyncMonsterPositionRoutine());
#endif
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("Despawn monster");
    }

    private IEnumerator ResyncMonsterPositionRoutine()
    {
        while (true)
        {
            // tant quâ€™on pousse, on ne corrige pas la position
            if (!physicsMonster.IsPushing && agent.enabled)
            {
                if (!agent.isOnNavMesh)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(agent.transform.position, out hit, 6f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        Debug.LogWarning("Aucune position NavMesh trouvÃ©e autour du monstre.");
                }
            }
            yield return new WaitForSeconds(2.5f);
            if (!physicsMonster.IsPushing && agent.enabled)
                physicsMonster.SyncMonsterPositionClientRpc(agent.transform.position);
        }
    }

    private void Update()
    {
        if (monsterReference != null)
        {
            if (monsterReference.playerStatistics.StunImmunitySeconds > 0f)
                monsterReference.playerStatistics.StunImmunitySeconds -= Time.deltaTime;

            bool stunned =
                monsterReference.playerStatistics.StunAirSeconds > 0f ||
                monsterReference.playerStatistics.StunSeconds > 0f ||
                monsterReference.playerStatistics.FreezeSeconds > 0f ||
                monsterReference.playerStatistics.SleepSeconds > 0f ||
                monsterReference.playerStatistics.ParaSeconds > 0f;

            if (stunned)
            {
                stunAccumulated += Time.deltaTime;
                if (stunAccumulated >= 5f)
                {
                    monsterReference.playerStatistics.StunSeconds = 0f;
                    monsterReference.playerStatistics.StunAirSeconds = 0f;
                    monsterReference.playerStatistics.FreezeSeconds = 0f;
                    monsterReference.playerStatistics.SleepSeconds = 0f;
                    monsterReference.playerStatistics.ParaSeconds = 0f;
                    monsterReference.playerStatistics.StunImmunitySeconds = 2.5f;
                    stunAccumulated = 0f;
                }
            }
            else
            {
                stunAccumulated = 0f;
            }
        }

        SpeedStateChanging();
        if (IsMovementBlocked()) agent.speed = 0;

        if (!Run.instance.CPUcontroller.zone.isGameEnded)
        {
            // Prefer heart target if event active
            if (SurvivorModifiers.defendStoneHeartThisWave && StoneHeartTarget.Instance != null)
            {
                defendTarget = StoneHeartTarget.Instance.transform;
            }
            else
            {
                defendTarget = null;
            }

            if (IsServer && defendTarget == null && !IsMovementBlocked())
            {
                targetEvaluationTimer += Time.deltaTime;
                if (cible == null || targetEvaluationTimer >= 1f)
                {
                    FindNearestTarget();
                    targetEvaluationTimer = 0f;
                }
            }
            if (!IsMovementBlocked() && !physicsMonster.IsPushing && agent.enabled && (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer) && cible == null && GameController.instance.playerReference.playerStatistics.playerInstanciated)
            {
                CombatMonster.AskNearestTargetServerRpc();
            }
        }

        if (TargetXform != null)
        {
            if (cible != null && (cible.playerStatistics.playerStatData.health <= 0f || !cible.playerStatistics.playerInstanciated))
            {
                cible = null;
                if (agent.isActiveAndEnabled) agent.ResetPath();
#if UNITY_SERVER
					CombatMonster.ApplyNullCibleClientRpc();
#endif
            }
            if (TargetXform != null)
            {
                float distance = Vector3.Distance(TargetXform.position, agent.transform.position);

                // IMPORTANT: on empÃªche tout cast pendant push/hold
                if (IsServer && !physicsMonster.IsPushing && !isRetreat)
                {
                    if (Time.time > CombatMonster.lastTimeSpellUsed + CombatMonster.timeBetweenSpells)
                    {
                        CombatMonster.TryCastSpell(distance);
                    }
                }

                if (agent.isActiveAndEnabled && !physicsMonster.IsPushing && !IsMovementBlocked())
                {
                    float preferredDistance = originalStoppingDistance * 0.9f;

                    if (agent.isActiveAndEnabled && !physicsMonster.IsPushing && !IsMovementBlocked())
                    {
                        if (distance < preferredDistance * CombatMonster.retreatThresholdRatio && doBackward)
                        {
                            isRetreat = true;
                            agent.speed = originalSpeed / 2f;
                            if (dodgeMonster != null)
                                dodgeMonster.TriggerSmartRetreat();
                            else
                                RetreatFromTarget();   // fallback si DodgeMonster absent
                        }
                        else if (distance > preferredDistance * CombatMonster.approachThresholdRatio)
                        {
                            if (!agent.isOnNavMesh)
                            {
                                StartCoroutine(AttemptRepositionToNavMesh());
                            }
                            else
                            {
                                agent.speed = originalSpeed;
                                agent.SetDestination(TargetXform.position);
                                isRetreat = false;
                            }
                        }
                        else
                        {
                            // Zone idÃ©ale
                            agent.ResetPath();
                            isRetreat = false;
                            animator.SetBool("isWalking", false);
                        }
                    }
                }
            }
        }

        if (cibleur != null && TargetXform != null)
        {
            cibleur.transform.LookAt(TargetXform);
        }

        if (monsterReference == null)
            return;

        // S'il est dans une position d'arrÃªt
        if (monsterReference.playerStatistics.isBehindWho == null && !agent.enabled && !physicsMonster.IsPushing) agent.enabled = true;

        if (IsMovementBlocked())
        {
            if (agent.isActiveAndEnabled) agent.ResetPath();

            if (monsterReference.playerStatistics.isBehindWho != null)
            {
                agent.enabled = false;
                agent.transform.position = monsterReference.playerStatistics.isBehindWho.transform.position;
            }
            animator.SetBool("isWalking", false);
#if UNITY_SERVER
				if (monsterReference.playerStatistics.isBehindWho != null || monsterReference.playerStatistics.playerStatData.health <= 0f)
				{
					CombatMonster.ApplyNullCibleClientRpc();
					cible = null;
				}
#endif

            if (monsterReference.playerStatistics.playerStatData.health <= 0f && !isDead)
            {
                isDead = true;
#if !UNITY_SERVER
                animator.Play("dead", 0, 0f);
                Instantiate(monsterReference.playerClasses.deathEffect, agent.transform.position, Quaternion.identity);
                CapsuleCollider col = agent.GetComponent<CapsuleCollider>();
                if (col != null) Destroy(col);
#endif

                if (Run.instance.CPUcontroller != null) Run.instance.CPUcontroller.zone.monsterNumber -= 1;
#if UNITY_SERVER
					NetworkManager.Destroy(monsterReference.gameObject, 2f);
#endif
            }
        }
        else
        {
            if (IsClient)
            {
                if (isDead) return;
                float movementThreshold = 0.01f;
                if (Vector3.Distance(agent.transform.position, previousPosition) > movementThreshold)
                {
                    stationaryTimer = 0f;
                    animator.SetBool("isWalking", true);
                    previousPosition = agent.transform.position;
                }
                else
                {
                    stationaryTimer += Time.deltaTime;
                    if (stationaryTimer > 0.2f)
                    {
                        animator.SetBool("isWalking", false);
                    }
                }
            }

            if (agent.enabled && !IsMovementBlocked())
                FaceTarget();
        }
        //Debug.Log("TargetXform: ",TargetXform);
    }

    private IEnumerator AttemptRepositionToNavMesh()
    {
        float maxTime = 2f;
        float elapsed = 0f;

        while (!agent.isOnNavMesh && elapsed < maxTime)
        {
            if (NavMesh.SamplePosition(agent.transform.position + Vector3.down * 0.5f, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                yield break;
            }
            else
            {
                agent.transform.position += Vector3.down * 7f * Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void RetreatFromTarget(float retreatDistance = 2f)
    {
        if (TargetXform == null || !agent.enabled || IsMovementBlocked()) return;

        Vector3 targetDirection = agent.transform.position - TargetXform.position;
        targetDirection.y = 0f;

        if (targetDirection == Vector3.zero) return;

        Vector3 retreatDirection = targetDirection.normalized;
        Vector3 retreatTargetPosition = agent.transform.position + retreatDirection * retreatDistance;

        if (NavMesh.SamplePosition(retreatTargetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            Debug.DrawLine(agent.transform.position, hit.position, Color.blue, 0.5f);
            agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning("No valid NavMesh point found for retreat.");
        }
    }

    private void FaceTarget()
    {
        if (TargetXform == null) return;
        Vector3 targetDirection = TargetXform.position - agent.transform.position;
        targetDirection.y = 0f;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 1.5f);
        }
    }

    private void FaceBack()
    {
        if (cible != null) return;
        Vector3 targetDirection = defaultPosition - agent.transform.position;
        targetDirection.y = 0f;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, targetRotation, Time.deltaTime * 1.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (monsterReference == null || monsterReference.playerStatistics == null) return;
        if (other.gameObject.layer == 7 && monsterReference.playerStatistics.NotAttackMonsters) return;
        if (activateFaceBack != null) StopCoroutine(activateFaceBack);

    }



#if UNITY_SERVER
	private void OnTriggerStay(Collider other)
	{
		if (monsterReference == null || monsterReference.playerStatistics == null) return;
		if (other.gameObject.layer == 7 && monsterReference.playerStatistics.NotAttackMonsters) return;
		if (activateFaceBack != null) StopCoroutine(activateFaceBack);

	}
#endif

    private void OnTriggerExit(Collider other)
    {
        if (cible != null && other.gameObject == cible && Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale)
        {
            activateFaceBack = null;
            cible = null;
            activateFaceBack = StartCoroutine(PrepareFaceBack());
        }
    }

    private IEnumerator PrepareFaceBack()
    {
        yield return new WaitForSeconds(1.25f);
        if (cible == null)
        {
            if (IsServer && agent.isActiveAndEnabled)
                agent.SetDestination(defaultPosition);
            FaceBack();
        }
    }

    public void FindNearestTarget()
    {
        PlayerReference bestTarget = null;
        PlayerReference closestTarget = null;
        PlayerReference closestLowHealthTarget = null;

        float minDistance = float.MaxValue;
        float minLowHealthDistance = float.MaxValue;
        float maxDistanceForLowHealth = 25f; // Portée max pour aller achever quelqu'un

        PlayerReference[] candidates = FindObjectsOfType<PlayerReference>();

        foreach (var candidate in candidates)
        {
            // 1. Sécurités de base
            if (candidate.playerStatistics == null) continue;
            if (!candidate.playerStatistics.playerInstanciated) continue;
            if (candidate.playerStatistics.playerStatData.health <= 0f) continue;
            if (monsterReference != null && monsterReference == candidate) continue;
            if (monsterReference != null && PlayerStatistics.AreAllies(monsterReference, candidate)) continue;

            // 2. Calcul de la distance
            float rawDistance = Vector3.Distance(candidate.transform.position, agent.transform.position);
            float distanceForEval = rawDistance;

            // 3. LE FOCUS : Si c'est déjà ma cible, je lui donne un "bonus" de proximité de 3 mètres
            // Cela empêche le bot de changer de cible frénétiquement si deux guerriers avancent côte à côte
            if (cible == candidate) 
            {
                distanceForEval -= 3f;
            }

            // Règle A : La cible globale la plus proche
            if (distanceForEval < minDistance)
            {
                minDistance = distanceForEval;
                closestTarget = candidate;
            }

            // Règle B : Opportunité d'achèvement (Cible à moins de 25% de vie)
            float maxHp = candidate.playerStatistics.playerStatData.maxHealth;
            float currentHp = candidate.playerStatistics.playerStatData.health;
            
            if (maxHp > 0 && (currentHp / maxHp) <= 0.25f && rawDistance <= maxDistanceForLowHealth)
            {
                if (distanceForEval < minLowHealthDistance)
                {
                    minLowHealthDistance = distanceForEval;
                    closestLowHealthTarget = candidate;
                }
            }
        }

        // LE CHOIX : On prend le blessé en priorité, sinon on prend le plus proche
        bestTarget = closestLowHealthTarget != null ? closestLowHealthTarget : closestTarget;

        // On applique la nouvelle cible si elle est différente de l'actuelle
        if (bestTarget != null && cible != bestTarget)
        {
            cible = bestTarget;
            CombatMonster.ApplyCibleClientRpc(cible.networkObject, agent.transform.position);
        }
    }

    private void SpeedStateChanging()
    {
        float factor = 1f;
        if (movementSpeedFactors != null)
        {
            foreach (var f in movementSpeedFactors.ToList())
            {
                if (f[2] + f[1] <= Time.time)
                    movementSpeedFactors.Remove(f);
                else
                    factor *= f[0];
            }
        }
        agent.speed = originalSpeed * factor;
    }

    public bool IsMovementBlocked()
    {
        return
            monsterReference == null ||
            monsterReference.playerStatistics.isBehindWho != null ||
            monsterReference.playerStatistics.playerStatData.health <= 0f ||
            monsterReference.playerStatistics.StunAirSeconds > 0f ||
            monsterReference.playerStatistics.StunSeconds > 0f ||
            monsterReference.playerStatistics.FreezeSeconds > 0f ||
            monsterReference.playerStatistics.SleepSeconds > 0f ||
            monsterReference.playerStatistics.ParaSeconds > 0f ||
            physicsMonster.isInBlockMove;
    }

}

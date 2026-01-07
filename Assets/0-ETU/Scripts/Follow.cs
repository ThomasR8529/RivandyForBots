using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Bcpg;
using PhysicsBasedCharacterController;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using SurvivorMode;
using UnityEngine.AI;

public class Follow : NetworkBehaviour
{
	[SerializeField] private NavMeshAgent agent;

	private Animator animator;
	private PlayerReference monsterReference;

	public bool isDead;

	[Header("Cibleur: le transform qui gÃ¨re le lancer pour tirer le projectile")]
	public Transform cibleur;

	[Header("doBackward: est-ce-que le monstre doit reculer s'il est au corps Ã  corps ? ")]
	public bool doBackward;

	// Layers que ce monstre peut cibler (joueurs, monstres ou couches custom)
	[SerializeField] private LayerMask targetableLayers = (1 << 9) | (1 << 3) | (1 << 5);

	public PlayerReference cible;
	// Optional non-player target (e.g., Stone Heart)
	[HideInInspector] public Transform defendTarget;
	private Transform TargetXform => (cible != null ? cible.transform : defendTarget);

	[SerializeField] private float timeBetweenSpells = 2.5f;
	[SerializeField] private float timeBetweenAutoAttacks = 2f;
	private float lastTimeAutoAttackUsed;

	[SerializeField, HideInInspector] private float lastTimeSpellUsed;

	[HideInInspector, SerializeField] public Vector3 defaultPosition;

	[SerializeField] public float rotationSpeed = 6f;

	private Rigidbody rigidBody;
	private CapsuleCollider capsule;

	private Vector3 previousPosition;

	public float maxSpellRange = 50f;

	private Coroutine activateFaceBack;
	private float stationaryTimer = 0f;

	// ---- Push management (un seul contrÃ´leur + pile d'impulsions) ----
	private Coroutine pushCoroutine;

	[HideInInspector] public bool isInBlockMove = false;

	public Billboard billboardEntity;
	public string twitchName;

	private float originalStoppingDistance;
	private float originalSpeed;

	public List<List<float>> movementSpeedFactors;

	[HideInInspector] public bool blockCast;

	[SerializeField] private float retreatThresholdRatio = 0.85f;
	[SerializeField] private float approachThresholdRatio = 1.15f;

	private bool isRetreat;
	private float stunAccumulated;

	// ---------- NEW: gestion concurrente des pushes ----------
	private struct PushEntry
	{
		public Vector3 velocity;  // units/s
		public float endTime;     // Time.time quand ce push expire
	}

	private readonly List<PushEntry> activePushes = new List<PushEntry>(4);
	private bool pushFirstWarpDone = false;  // le premier push fait le warp de dÃ©part
	private Vector3 cachedStartPos;
	private Quaternion cachedStartRot;

	// sauvegarde Ã©tat collisions/physique pendant le push
	private bool prevKinematic;
	private bool IsPushing => pushCoroutine != null;

	// FenÃªtre tampon pour absorber les micro-trous entre pushes
	[SerializeField] private float waitBeforeNormalState = 0.3f; // 120 ms de tampon
	private float pushReleaseAt = 0f;                       // Time.time Ã  partir duquel on peut relÃ¢cher blockCast
	private bool IsPushLocked => blockCast || IsPushing || Time.time < pushReleaseAt;
	// ----------------------------------------------------------

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
			if (!IsPushing && agent.enabled)
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
			if (!IsPushing && agent.enabled)
				SyncMonsterPositionClientRpc(agent.transform.position);
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

			if (IsServer && (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer) && cible == null && defendTarget == null && !IsMovementBlocked())
			{
				FindNearestTarget();
			}
			if (!IsMovementBlocked() && !IsPushing && agent.enabled && (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer) && cible == null && GameController.instance.playerReference.playerStatistics.playerInstanciated)
			{
				AskNearestTargetServerRpc();
			}
		}

		if (TargetXform != null)
		{
			if (cible != null && (cible.playerStatistics.playerStatData.health <= 0f || !cible.playerStatistics.playerInstanciated))
			{
				cible = null;
				if (agent.isActiveAndEnabled) agent.ResetPath();
#if UNITY_SERVER
				ApplyNullCibleClientRpc();
#endif
			}
			if (TargetXform != null)
			{
				float distance = Vector3.Distance(TargetXform.position, agent.transform.position);

				// IMPORTANT: on empÃªche tout cast pendant push/hold
				if (IsServer && !IsPushLocked && !isRetreat)
				{
					if (Time.time > lastTimeSpellUsed + timeBetweenSpells)
					{
						TryCastSpell(distance);
					}
				}

				if (agent.isActiveAndEnabled && !IsPushing && !IsMovementBlocked())
				{
					float preferredDistance = originalStoppingDistance;

					if (agent.isActiveAndEnabled && !IsPushing && !IsMovementBlocked())
					{
						if (distance < preferredDistance * retreatThresholdRatio && doBackward)
						{
							isRetreat = true;
							agent.speed = originalSpeed / 2f;
							RetreatFromTarget();
						}
						else if (distance > preferredDistance * approachThresholdRatio)
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
		if (monsterReference.playerStatistics.isBehindWho == null && !agent.enabled && !IsPushing) agent.enabled = true;

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
				ApplyNullCibleClientRpc();
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
			transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, targetRotation, Time.deltaTime * 1.5f);
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
#if UNITY_SERVER
		PlayerReference otherRef = other.GetComponent<PlayerReference>();
		if (monsterReference != null && otherRef != null && PlayerStatistics.AreAllies(monsterReference, otherRef))
		{
			return;
		}

		if (cible == null && otherRef != null)
		{
			cible = otherRef;
			ApplyCibleClientRpc(cible.networkObject, agent.transform.position);
		}
#endif
	}

#if UNITY_SERVER
	private void OnTriggerStay(Collider other)
	{
		if (monsterReference == null || monsterReference.playerStatistics == null)
		{
			return;
		}
		if (other.gameObject.layer == 7 && monsterReference.playerStatistics.NotAttackMonsters)
		{
			return;
		}
		if (activateFaceBack != null)
			StopCoroutine(activateFaceBack);

		PlayerReference otherRef = other.GetComponent<PlayerReference>();
		if (monsterReference != null && otherRef != null && PlayerStatistics.AreAllies(monsterReference, otherRef))
		{
			return;
		}
		if (cible == null && otherRef != null)
		{
			cible = otherRef;
			ApplyCibleClientRpc(cible.networkObject, agent.transform.position);
		}
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

	private void CheckSpellExecution(float distance)
	{
		// SÃ©curitÃ© supplÃ©mentaire si cette voie est encore utilisÃ©e quelque part
		if (monsterReference == null || monsterReference.playerShooting == null || monsterReference.playerClasses == null || IsMovementBlocked() || IsPushLocked)
			return;

		lastTimeSpellUsed = Time.time;

		var spells = monsterReference.playerClasses.spells;
		float defaultRange = originalStoppingDistance + 3f;
		float[] spellCooldowns = {
			monsterReference.playerClasses.warriorSpell1CD,
			monsterReference.playerClasses.warriorSpell2CD,
			monsterReference.playerClasses.warriorSpell3CD,
			monsterReference.playerClasses.warriorSpell4CD
		};

		for (int i = 0; i < spells.Length && i < spellCooldowns.Length; i++)
		{
			float currentSpellRange = spells[i].spellRange != 0 ? spells[i].spellRange : defaultRange;

			if (spellCooldowns[i] <= 0f && distance < currentSpellRange && distance < maxSpellRange)
			{
				monsterReference.playerShooting.ExecuteSpell(i, 0f, isPlayer: false);
				break;
			}
		}
	}

	public NavMeshAgent GetAgent() => agent;

	[ClientRpc]
	public void ApplyCibleClientRpc(NetworkObjectReference netCible, Vector3 positionToApply, ClientRpcParams _ = default)
	{
		if (netCible.TryGet(out NetworkObject casterNet))
		{
			Debug.Log("ApplyCibelClientRpc");
			cible = casterNet.gameObject.GetComponent<PlayerReference>();
			// ne warp pas si on est en cours de push
			if (!IsPushing && agent.enabled)
				agent.Warp(positionToApply);
		}
	}

	[ClientRpc]
	public void ApplyNullCibleClientRpc()
	{
		cible = null;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AskNearestTargetServerRpc(ServerRpcParams serverRpcParams = default)
	{
		if (cible == null)
		{
			FindNearestTarget();
		}
		Debug.Log("AskNearestTargetServerRpc 1");
		if (cible == null) return;
		Debug.Log("AskNearestTargetServerRpc 2");
		ClientRpcParams clientRpcParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
			}
		};
		Debug.Log("AskNearestTargetServerRpc 3");
		ApplyCibleClientRpc(cible.networkObject, agent.transform.position, clientRpcParams);
	}

	private void FindNearestTarget()
	{
		if (cible != null)
			return;

		float minDistance = float.MaxValue;
		PlayerReference nearestTarget = null;
		PlayerReference[] candidates = FindObjectsOfType<PlayerReference>();

		foreach (var candidate in candidates)
		{
			if (monsterReference != null && monsterReference == candidate)
				continue;
			if (monsterReference != null && monsterReference.playerStatistics != null && candidate.playerStatistics != null && monsterReference.playerStatistics.IsSameTeam(candidate.playerStatistics))
				continue;

			float distance = Vector3.Distance(candidate.transform.position, agent.transform.position);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearestTarget = candidate;
			}
		}

		if (nearestTarget != null)
		{
			cible = nearestTarget;
			ApplyCibleClientRpc(cible.networkObject, agent.transform.position);
		}
	}

	[ClientRpc]
	public void SyncMonsterPositionClientRpc(Vector3 position)
	{
		// ignorer sync pendant un push
		if (!IsPushing && agent.enabled) agent.Warp(position);
		if (!agent.isOnNavMesh)
		{
			NavMeshHit hit;
			if (NavMesh.SamplePosition(agent.transform.position, out hit, 6f, NavMesh.AllAreas))
			{
				agent.Warp(hit.position);
			}
			else
			{
				Debug.LogWarning("Aucune position NavMesh trouvÃ©e autour du monstre.");
			}
		}
	}

	// ======== API publique appelÃ©e par les sorts ========

	public void TriggerPush(Vector3 pushDirection, float pushDuration)
	{
		// convertir en vitesse monde (lâ€™ancienne implÃ©mentation multipliait par 0.8f Ã  chaque frame)
		Vector3 velocity = pushDirection * 0.8f;

#if UNITY_SERVER
		TriggerPushClientRpc(velocity, pushDuration, agent.transform.position, agent.transform.rotation);
#endif
		EnqueuePush(velocity, pushDuration, agent.transform.position, agent.transform.rotation);
	}

	[ClientRpc]
	public void TriggerPushClientRpc(Vector3 velocity, float pushDuration, Vector3 startPosition, Quaternion startRotation)
	{
		EnqueuePush(velocity, pushDuration, startPosition, startRotation);
	}

	// ======== Coeur de la nouvelle gestion des pushes ========

	private void EnqueuePush(Vector3 velocity, float duration, Vector3 startPosition, Quaternion startRotation)
	{
		float endTime = Time.time + Mathf.Max(0.01f, duration);
		activePushes.Add(new PushEntry { velocity = velocity, endTime = endTime });

		// Prolonge la fenÃªtre d'interdiction de cast
		pushReleaseAt = Mathf.Max(pushReleaseAt, endTime + waitBeforeNormalState);

		// IMPORTANT: sÃ©curiser immÃ©diatement sans attendre le coroutine
		blockCast = true;
		Debug.Log("BLOCK CAST: TRUE");

		if (!IsPushing)
		{
			pushFirstWarpDone = false;
			cachedStartPos = startPosition;
			cachedStartRot = startRotation;
			pushCoroutine = StartCoroutine(PushController());
		}
	}

	private IEnumerator PushController()
	{
		// 1) PrÃ©paration: un seul warp + dÃ©sactivation agent + geler collisions pour Ã©viter frottements
		if (!pushFirstWarpDone)
		{
			if (rigidBody == null) rigidBody = GetComponent<Rigidbody>();
			if (capsule == null) capsule = (agent != null ? agent.GetComponent<CapsuleCollider>() : null) ?? GetComponent<CapsuleCollider>();
			if (rigidBody != null)
			{
				prevKinematic = rigidBody.isKinematic;
				rigidBody.isKinematic = true; // on fige, mais on ne touche pas Ã  detectCollisions
			}

			agent.transform.rotation = cachedStartRot;
			agent.Warp(cachedStartPos);
			agent.enabled = false;
			blockCast = true; // redondant mais explicite
			pushFirstWarpDone = true;
		}

		// 2) Boucle pendant quâ€™il reste des pushes actifs
		while (true)
		{
			// purge
			float now = Time.time;
			for (int i = activePushes.Count - 1; i >= 0; --i)
				if (activePushes[i].endTime <= now)
					activePushes.RemoveAt(i);

			// plus de pushes actifs ?
			if (activePushes.Count == 0)
			{
				// on garde blockCast tant que la fenÃªtre "hold" n'est pas Ã©coulÃ©e
				if (Time.time < pushReleaseAt)
				{
					yield return null;
					continue;
				}
				break; // fin de push: on pourra restaurer et mettre blockCast=false
			}

			// somme des vitesses + dÃ©placement
			Vector3 totalVelocity = Vector3.zero;
			for (int i = 0; i < activePushes.Count; i++)
				totalVelocity += activePushes[i].velocity;

			Vector3 currentPosition = agent.transform.position;
			Vector3 nextPosition = currentPosition + totalVelocity * Time.deltaTime;

			// sÃ©curitÃ© anti-traversÃ©e sol si on descend
			if (totalVelocity.y < 0f)
			{
				Vector3 origin = currentPosition + Vector3.up * 0.1f;
				float distance = (nextPosition - currentPosition).magnitude + 0.1f;
				if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, LayerMask.GetMask("Ground")))
				{
					for (int i = activePushes.Count - 1; i >= 0; --i)
						if (activePushes[i].velocity.y < 0f)
							activePushes.RemoveAt(i);
					yield return null;
					continue;
				}
			}

			// CapsuleCast sweep with simple sliding against obstacles
			int obstacleMask = LayerMask.GetMask("Default", "Ground");
			float skin = 0.02f;
			int maxSlides = 2;
			Vector3 delta = nextPosition - currentPosition;
			Vector3 newPos = currentPosition;
			if (delta.sqrMagnitude > 1e-8f)
			{
				GetCapsuleWorld(capsule, out Vector3 baseP1, out Vector3 baseP2, out float capRadius);
				Vector3 offset = newPos - agent.transform.position;
				Vector3 p1 = baseP1 + offset;
				Vector3 p2 = baseP2 + offset;

				for (int i = 0; i < maxSlides && delta.sqrMagnitude > 1e-8f; i++)
				{
					Vector3 dir = delta.normalized;
					float dist = delta.magnitude;
					if (Physics.CapsuleCast(p1, p2, capRadius, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
					{
						float moveDist = Mathf.Max(hit.distance - skin, 0f);
						newPos += dir * moveDist;
						delta -= dir * moveDist;
						// Slide along the hit surface
						delta = Vector3.ProjectOnPlane(delta, hit.normal);
						// Recompute capsule start points at the new position (rotation remains unchanged during push)
						offset = newPos - agent.transform.position;
						p1 = baseP1 + offset;
						p2 = baseP2 + offset;
					}
					else
					{
						newPos += dir * dist;
						delta = Vector3.zero;
					}
				}

				agent.transform.position = newPos;
			}
			else
			{
				agent.transform.position = nextPosition;
			}
			yield return null;
		}

		// 3) Reposer sur le sol si besoin
		float safetyTimer = 0f;
		while (!IsGrounded() && safetyTimer < 3f)
		{
			agent.transform.position += Vector3.down * 7f * Time.deltaTime;
			safetyTimer += Time.deltaTime;
			yield return null;
		}

		// 4) Restaurations & fin

		if (rigidBody != null) rigidBody.isKinematic = prevKinematic;

		if (!agent.enabled) agent.enabled = true;
		blockCast = false;

		if (!agent.isOnNavMesh)
		{
			if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
				agent.Warp(hit.position);
			else
				Debug.LogWarning("Pas de NavMesh trouvÃ© pour re-warp.");
		}

		pushCoroutine = null;

		if (cible == null) AskNearestTargetServerRpc();
	}

	// ======================================================

	// Compute world capsule endpoints and radius for sweeping.
	private void GetCapsuleWorld(CapsuleCollider col, out Vector3 p1, out Vector3 p2, out float radius)
	{
		if (col == null)
		{
			Vector3 basePos = (agent != null ? agent.transform.position : transform.position);
			radius = 0.4f;
			p1 = basePos + Vector3.up * 0.4f;
			p2 = basePos + Vector3.up * 1.2f;
			return;
		}

		Transform t = col.transform;
		Vector3 center = t.TransformPoint(col.center);
		Vector3 s = t.lossyScale;
		int dir = col.direction; // 0=X, 1=Y, 2=Z
		float axisScale = (dir == 0 ? Mathf.Abs(s.x) : (dir == 1 ? Mathf.Abs(s.y) : Mathf.Abs(s.z)));
		float perpA = (dir == 0 ? Mathf.Abs(s.y) : Mathf.Abs(s.x));
		float perpB = (dir == 2 ? Mathf.Abs(s.y) : Mathf.Abs(s.z));
		radius = col.radius * Mathf.Max(perpA, perpB);
		float heightWorld = Mathf.Max(col.height * axisScale, radius * 2f);
		float halfCylinder = Mathf.Max(0f, (heightWorld * 0.5f) - radius);
		Vector3 axis = (dir == 0 ? t.right : (dir == 1 ? t.up : t.forward));
		p1 = center + axis * halfCylinder;
		p2 = center - axis * halfCylinder;
	}

	private bool TryCastSpell(float distance)
	{
		if (monsterReference == null || monsterReference.playerShooting == null || monsterReference.playerClasses == null || IsMovementBlocked() || IsPushLocked)
			return false;

		var spells = monsterReference.playerClasses.spells;
		float defaultRange = originalStoppingDistance + 3f;
		float[] spellCooldowns = {
			monsterReference.playerClasses.warriorSpell1CD,
			monsterReference.playerClasses.warriorSpell2CD,
			monsterReference.playerClasses.warriorSpell3CD,
			monsterReference.playerClasses.warriorSpell4CD
		};

		for (int i = 0; i < spells.Length && i < spellCooldowns.Length; i++)
		{
			float currentSpellRange = spells[i].spellRange != 0 ? spells[i].spellRange : defaultRange;

			if (spellCooldowns[i] <= 0f && distance < currentSpellRange && distance < maxSpellRange)
			{
				monsterReference.playerShooting.ExecuteSpell(i, 0f, isPlayer: false);
				lastTimeSpellUsed = Time.time;
				lastTimeAutoAttackUsed = Time.time;
				return true;
			}
		}
		return false;
	}

	private void TryAutoAttack(float distance)
	{
		if (monsterReference == null || monsterReference.playerShooting == null || monsterReference.playerClasses == null || IsMovementBlocked() || IsPushLocked)
			return;

		var autoAttacks = monsterReference.playerClasses.autoAttacks;
		float defaultRange = originalStoppingDistance + 3f;

		for (int i = 0; i < autoAttacks.Length; i++)
		{
			if (autoAttacks[i] != null)
			{
				float range = autoAttacks[i].spellRange != 0 ? autoAttacks[i].spellRange : defaultRange;
				if (distance < range && distance < maxSpellRange)
				{
					lastTimeSpellUsed = Time.time;
					lastTimeAutoAttackUsed = Time.time;
					Vector3 aimPos = TargetXform != null ? TargetXform.position : agent.transform.position;
					monsterReference.playerShooting.LaunchSpellGlobal(monsterReference.gameObject, i, aimPos, true);
					monsterReference.playerShooting.LaunchSpellClientRpc(monsterReference.gameObject, i, true, aimPos);
					break;
				}
			}
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

	public bool IsGrounded(float distanceToGround = 0.3f, float sphereRadius = 0.25f)
	{
		Vector3 origin = monsterReference.transform.position + Vector3.up * 0.2f; // lÃ©gÃ¨rement relevÃ©
		float castDistance = distanceToGround + 0.2f;

		return Physics.SphereCast(
			origin,
			sphereRadius,
			Vector3.down,
			out RaycastHit hit,
			castDistance,
			LayerMask.GetMask("Ground", "Default")
		);
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
			isInBlockMove;
	}

	// API utilitaire : utilise la pile de pushs
	public void MoveTowardsFromPoint(Vector3 point, float power, float seconds = 0.25f)
	{
		// Direction : du point -> monstre (sâ€™Ã©loigner du point)
		Vector3 dir = agent.transform.position - point;
		dir.y = 0f;
		dir.Normalize();

		// power < 0 => aller vers le point
		if (power < 0f)
		{
			dir = -dir;
			power = -power;
		}

		Vector3 pushVelocity = dir * power * 0.45f;

#if UNITY_SERVER
		EnqueuePush(pushVelocity, (seconds > 0f ? seconds : 0.2f), agent.transform.position, agent.transform.rotation);
		TriggerPushClientRpc(pushVelocity, (seconds > 0f ? seconds : 0.2f), agent.transform.position, agent.transform.rotation);
#else
		EnqueuePush(pushVelocity, (seconds > 0f ? seconds : 0.2f), agent.transform.position, agent.transform.rotation);
#endif
	}

	// helper si tu as la rÃ©fÃ©rence du â€œcasterâ€
	public void MoveTowardsCaster(PlayerReference caster, float power, float seconds = 0.25f)
	{
		if (caster == null) return;
		MoveTowardsFromPoint(caster.transform.position, power, seconds);
	}
}


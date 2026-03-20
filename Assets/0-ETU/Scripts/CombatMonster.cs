using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using SurvivorMode;
using PhysicsBasedCharacterController;
using Game;

[RequireComponent(typeof(Follow))]
[RequireComponent(typeof(PhysicsMonster))]

public class CombatMonster : NetworkBehaviour

{
    private Follow follow;
    private PhysicsMonster physicsMonster;

    [SerializeField] public float timeBetweenSpells = 2.5f;
    [SerializeField] public float timeBetweenAutoAttacks = 2f;

    [SerializeField] public float retreatThresholdRatio = 0.85f;
 	[SerializeField] public float approachThresholdRatio = 1.15f;

    private float lastTimeAutoAttackUsed;
    [SerializeField, HideInInspector] public float lastTimeSpellUsed;

    public float maxSpellRange = 50f;
    [HideInInspector] public bool blockCast;

	// Anti-spam : mémorise le dernier sort lancé pour ne pas toujours choisir le slot 0
    private int lastSpellUsedIndex = -1;
    // Empêche d'empiler plusieurs casts simultanés
    private bool isCastPending = false;

    [Header("=== Cast intelligent ===")]
    [Tooltip("Délai humain min (s) avant de lancer un sort")]
    [SerializeField] private float humanDelayMin = 0.05f;
    [Tooltip("Délai humain max (s) avant de lancer un sort")]
    [SerializeField] private float humanDelayMax = 0.30f;
    [Tooltip("Pénalité sur le score du dernier sort utilisé (0=jamais rejoué, 1=pas de pénalité)")]
    [SerializeField, Range(0f, 1f)] private float spamPenalty = 0.25f;
    [Tooltip("Boost de priorité si le monstre est à moins de 30% HP")]
    [SerializeField] private float lowHpCastBoost = 1.5f;

    private void Awake()
    {
        follow = GetComponent<Follow>();
        physicsMonster = GetComponent<PhysicsMonster>();
    }

    private void CheckSpellExecution(float distance)
	{
		// SÃ©curitÃ© supplÃ©mentaire si cette voie est encore utilisÃ©e quelque part
		if (follow.monsterReference == null || follow.monsterReference.playerShooting == null || follow.monsterReference.playerClasses == null || follow.IsMovementBlocked() || physicsMonster.IsPushLocked)
			return;

		lastTimeSpellUsed = Time.time;

		var spells = follow.monsterReference.playerClasses.spells;
		float defaultRange = follow.originalStoppingDistance + 3f;
		float[] spellCooldowns = {
			follow.monsterReference.playerClasses.warriorSpell1CD,
			follow.monsterReference.playerClasses.warriorSpell2CD,
			follow.monsterReference.playerClasses.warriorSpell3CD,
			follow.monsterReference.playerClasses.warriorSpell4CD
		};

		for (int i = 0; i < spells.Length && i < spellCooldowns.Length; i++)
		{
			float currentSpellRange = spells[i].spellRange != 0 ? spells[i].spellRange : defaultRange;

			if (spellCooldowns[i] <= 0f && distance < currentSpellRange && distance < maxSpellRange)
			{
				follow.monsterReference.playerShooting.ExecuteSpell(i, 0f, isPlayer: false);
				break;
			}
		}
	}

    public bool TryCastSpell(float distance)
	{
		if (follow.cible != null && PlayerStatistics.AreAllies(follow.monsterReference, follow.cible))
		{
			Debug.Log("SECURITY: ally target detected, cancel cast");
			follow.cible = null;
			return false;
		}
		if (follow.monsterReference == null || follow.monsterReference.playerShooting == null || follow.monsterReference.playerClasses == null || follow.IsMovementBlocked() || physicsMonster.IsPushLocked || (follow.dodgeMonster != null && follow.dodgeMonster.IsDodging))
			return false;

		if (isCastPending) return false;

		var spells = follow.monsterReference.playerClasses.spells;
		float defaultRange = follow.originalStoppingDistance + 3f;
		float[] spellCooldowns = {
			follow.monsterReference.playerClasses.warriorSpell1CD,
			follow.monsterReference.playerClasses.warriorSpell2CD,
			follow.monsterReference.playerClasses.warriorSpell3CD,
			follow.monsterReference.playerClasses.warriorSpell4CD
		};

		// ── Trouver le meilleur sort disponible par score ──
		int   bestIndex   = -1;
		float bestScore   = -1f;
		float selfHpRatio = follow.monsterReference.playerStatistics.playerStatData.maxHealth > 0f
			? follow.monsterReference.playerStatistics.playerStatData.health / follow.monsterReference.playerStatistics.playerStatData.maxHealth
			: 1f;

		for (int i = 0; i < spells.Length && i < spellCooldowns.Length; i++)
		{
			if (spells[i] == null) continue;
			if (spellCooldowns[i] > 0f) continue; // en cooldown

			float currentSpellRange = spells[i].spellRange != 0 ? spells[i].spellRange : defaultRange;

			if (spellCooldowns[i] <= 0f && distance < currentSpellRange && distance < maxSpellRange)
			{
				follow.monsterReference.playerShooting.ExecuteSpell(i, 0f, isPlayer: false);
				lastTimeSpellUsed = Time.time;
				lastTimeAutoAttackUsed = Time.time;
				return true;
			}
			//Debug.Log("Spell " + i + " CD = " + spellCooldowns[i]);
			//Debug.Log("distance = " + distance + " range = " + currentSpellRange);
		if (distance >= currentSpellRange || distance >= maxSpellRange) continue; // hors portée

			float score = 1f;

			// Pénalité anti-spam : le dernier sort utilisé est moins prioritaire
			if (i == lastSpellUsedIndex)
				score *= spamPenalty;

			// Bonus défensif si le monstre est en danger
			if (selfHpRatio < 0.3f)
				score *= lowHpCastBoost;

			// Légère randomisation pour casser la prévisibilité
			score *= Random.Range(0.85f, 1.15f);

			if (score > bestScore)
			{
				bestScore = score;
				bestIndex = i;
			}
		}

		if (bestIndex >= 0)
		{
			StartCoroutine(CastWithHumanDelay(bestIndex, spells, spellCooldowns, defaultRange));
			return true;
		}

		TryAutoAttack(distance);
    	return false;
	}

	// Lance le sort choisi après un court délai aléatoire
	private IEnumerator CastWithHumanDelay(int spellIndex, Spell[] spells, float[] spellCooldowns, float defaultRange)
	{
		isCastPending = true;

		yield return new WaitForSeconds(Random.Range(humanDelayMin, humanDelayMax));

		// Re-vérifications après le délai
		if (follow.monsterReference == null ||
		    follow.IsMovementBlocked() ||
		    physicsMonster.IsPushLocked ||
		    (follow.dodgeMonster != null && follow.dodgeMonster.IsDodging))
		{
			isCastPending = false;
			yield break;
		}

		// Recalcul des cooldowns au moment réel du cast
		float[] freshCooldowns = {
			follow.monsterReference.playerClasses.warriorSpell1CD,
			follow.monsterReference.playerClasses.warriorSpell2CD,
			follow.monsterReference.playerClasses.warriorSpell3CD,
			follow.monsterReference.playerClasses.warriorSpell4CD
		};

		if (spellIndex >= spells.Length || spells[spellIndex] == null)
		{
			isCastPending = false;
			yield break;
		}

		float currentDistance = follow.TargetXform != null
			? Vector3.Distance(follow.TargetXform.position, follow.agent.transform.position)
			: float.MaxValue;

		float range = spells[spellIndex].spellRange != 0 ? spells[spellIndex].spellRange : defaultRange;

		if (spellIndex < freshCooldowns.Length &&
		    freshCooldowns[spellIndex] <= 0f &&
		    currentDistance < range &&
		    currentDistance < maxSpellRange)
		{
			follow.monsterReference.playerShooting.ExecuteSpell(spellIndex, 0f, isPlayer: false);
			lastTimeSpellUsed      = Time.time;
			lastTimeAutoAttackUsed = Time.time;
			lastSpellUsedIndex     = spellIndex;
		}

        // Si c'est le sort de charge (slot 0), surveiller l'impact
            if (spellIndex == 0)
                follow.StartChargeImpactWatch(spellIndex);
		//isCastPending = false;
	}
    private void TryAutoAttack(float distance)
	{
		if (follow.cible != null && PlayerStatistics.AreAllies(follow.monsterReference, follow.cible))
		{
			Debug.Log("SECURITY: ally target detected, cancel cast");
			follow.cible = null;
			return;
		}
		
		if (follow.monsterReference == null || follow.monsterReference.playerShooting == null || follow.monsterReference.playerClasses == null || follow.IsMovementBlocked() || physicsMonster.IsPushLocked)
			return;

		var autoAttacks = follow.monsterReference.playerClasses.autoAttacks;
		float defaultRange = follow.originalStoppingDistance + 3f;

		for (int i = 0; i < autoAttacks.Length; i++)
		{
			if (autoAttacks[i] != null)
			{
				float range = autoAttacks[i].spellRange != 0 ? autoAttacks[i].spellRange : defaultRange;
				if (distance < range && distance < maxSpellRange)
				{
					lastTimeSpellUsed = Time.time;
					lastTimeAutoAttackUsed = Time.time;
					Vector3 aimPos = follow.TargetXform != null ? follow.TargetXform.position : follow.agent.transform.position;
					follow.monsterReference.playerShooting.LaunchSpellGlobal(follow.monsterReference.gameObject, i, aimPos, true);
					follow.monsterReference.playerShooting.LaunchSpellClientRpc(follow.monsterReference.gameObject, i, true, aimPos);
					break;
				}
			}
		}
	}

    [ClientRpc]
	public void ApplyCibleClientRpc(NetworkObjectReference netCible, Vector3 positionToApply, ClientRpcParams _ = default)
	{
		if (netCible.TryGet(out NetworkObject casterNet))
		{
			Debug.Log("ApplyCibelClientRpc");
			follow.cible = casterNet.gameObject.GetComponent<PlayerReference>();
			// ne warp pas si on est en cours de push
			if (!physicsMonster.IsPushing && follow.agent.enabled)
				follow.agent.Warp(positionToApply);
		}
	}

    [ClientRpc]
	public void ApplyNullCibleClientRpc()
	{
		follow.cible = null;
	}

    [ServerRpc(RequireOwnership = false)]
	public void AskNearestTargetServerRpc(ServerRpcParams serverRpcParams = default)
	{
		if (follow.cible == null)
		{
			follow.FindNearestTarget();
		}
		Debug.Log("AskNearestTargetServerRpc 1");
		if (follow.cible == null) return;
		Debug.Log("AskNearestTargetServerRpc 2");
		ClientRpcParams clientRpcParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
			}
		};
		Debug.Log("AskNearestTargetServerRpc 3");
		ApplyCibleClientRpc(follow.cible.networkObject, follow.agent.transform.position, clientRpcParams);
	}

    
}
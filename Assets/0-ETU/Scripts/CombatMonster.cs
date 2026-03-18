using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using SurvivorMode;
using PhysicsBasedCharacterController;

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
				lastTimeSpellUsed = Time.time;
				lastTimeAutoAttackUsed = Time.time;
				return true;
			}
			//Debug.Log("Spell " + i + " CD = " + spellCooldowns[i]);
			//Debug.Log("distance = " + distance + " range = " + currentSpellRange);
		}
		TryAutoAttack(distance);
    	return false;
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
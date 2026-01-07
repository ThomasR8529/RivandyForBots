using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using SurvivorMode;
using RealToon.Script;

public class PlayerReincarnation : NetworkBehaviour
{
	[SerializeField]
	private float lastTimeReincarned;

	[SerializeField]
	private bool isReincarnation;

	private GameObject reincarnationObj;

	[SerializeField] private GameObject reincarnationEffect;

	private PlayerReference playerReference;

	public bool IsReincarnation
	{
		get
		{
			return isReincarnation;
		}
		set
		{
			isReincarnation = value;
		}
	}


    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            playerReference = GetComponent<PlayerReference>();
            InputManager.inputActions.Player.Reincarnation.performed += ReincarnationAction;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsLocalPlayer)
        {
            try { InputManager.inputActions.Player.Reincarnation.performed -= ReincarnationAction; } catch {}
        }
        base.OnNetworkDespawn();
    }

    private void OnDisable()
    {
        if (IsLocalPlayer)
        {
            try { InputManager.inputActions.Player.Reincarnation.performed -= ReincarnationAction; } catch {}
        }
    }

	private void ReincarnationAction(InputAction.CallbackContext obj)
	{
		// Si reincarnation bloqu�e (didacticiel par exemple)
		if (!enabled)
			return;
		// Si on est � un etat impossible
		if (playerReference.playerStatistics.StunAirSeconds > 0f || playerReference.playerStatistics.StunSeconds > 0f || playerReference.playerStatistics.FreezeSeconds > 0f || playerReference.playerStatistics.SleepSeconds > 0f || playerReference.playerStatistics.ParaSeconds > 0f)
		{
			Debug.Log("Etat impossible");
			return;
		}
		if (playerReference.playerStatistics.ServerBlockSeconds > 0f)
		{
			Debug.Log("Le serveur bloque.");
			return;
		}

		// Survivor modifiers: client-side early block
		if (!isReincarnation && SurvivorModifiers.spiritTransformBlocked)
		{
			Debug.Log("Transformation esprit bloquée par un événement.");
			return;
		}
		if (isReincarnation && SurvivorModifiers.humanTransformBlocked)
		{
			Debug.Log("Retour en humain bloqué par un événement.");
			return;
		}

		AskForReincarnationServerRpc(gameObject);

	}

	public void Transformation(PlayerStruct data, PlayerReference playerReference)
	{
		if (!playerReference.PlayerReincarnation.IsReincarnation)
		{
			playerReference.PlayerReincarnation.IsReincarnation = true;
			playerReference.PlayerReincarnation.reincarnationObj = Instantiate(Resources.Load("Prefabs/Reincarnation/" + data.reId), playerReference.transform) as GameObject;
			playerReference.playerClasses.ChangeClass(10, playerReference.PlayerReincarnation.reincarnationObj, data.userId);
			if (playerReference.autoAttacks == null) playerReference.autoAttacks = playerReference.playerClasses.animator.GetComponent<AutoAttacks>();
			RebuildSmearTargets(playerReference, playerReference.PlayerReincarnation.reincarnationObj);
			playerReference.playerShooting?.HandleTransformationEvent();
#if UNITY_SERVER
			TransformationClientRpc(data, playerReference.gameObject);
#endif
		}
	}

	[ClientRpc]
	public void TransformationClientRpc(PlayerStruct data, NetworkObjectReference refObj)
	{
		if (refObj.TryGet(out NetworkObject playerNet))
		{
			PlayerReference playerReference = playerNet.GetComponent<PlayerReference>();
			if (!playerReference.PlayerReincarnation.isReincarnation)
			{
				playerReference.PlayerReincarnation.isReincarnation = true;
				Instantiate(reincarnationEffect, playerReference.transform.position, playerReference.transform.rotation);
				playerReference.effectUtils.HideDissolve(0.1f);
				playerReference.PlayerReincarnation.reincarnationObj = Instantiate(Resources.Load("Prefabs/Reincarnation/" + data.reId), playerReference.transform) as GameObject;
				playerReference.playerClasses.ChangeClass(10, reincarnationObj, data.userId);
				if (playerReference.autoAttacks == null) playerReference.autoAttacks = playerReference.playerClasses.animator.GetComponent<AutoAttacks>();
				RebuildSmearTargets(playerReference, playerReference.PlayerReincarnation.reincarnationObj);
				playerReference.playerShooting?.HandleTransformationEvent();

				if (playerReference.playerStatistics.ReincarnationPower >= 35f)
				{
					playerReference.playerStatistics.ReincarnationPower -= 35;
				}
				if (playerReference.IsLocalPlayer)
				{
					cooldownUI.instance.UpdateUiInformation();
					cooldownUI.instance.transformationEffectDirector.Play();
				}
				playerReference.effectUtils.ShowDissolve(1f);
			}
		}
	}

	public void DeTransformation(PlayerStruct data, PlayerReference playerReference)
	{
		if (playerReference.PlayerReincarnation.isReincarnation && playerReference.playerStatistics.playerStatData.health > 0f)
		{
			Destroy(playerReference.PlayerReincarnation.reincarnationObj);

			playerReference.playerClasses.ChangeClass(data.classId, null, data.userId);
			playerReference.PlayerReincarnation.isReincarnation = false;
			RebuildSmearTargets(playerReference, null);
			playerReference.playerShooting?.HandleTransformationEvent();
#if UNITY_SERVER
			DeTransformationClientRpc(data, playerReference.gameObject);
#endif
		}
	}

	[ClientRpc]
	public void DeTransformationClientRpc(PlayerStruct data, NetworkObjectReference refObj)
	{
		if (refObj.TryGet(out NetworkObject playerNet))
		{
			PlayerReference playerReference = playerNet.GetComponent<PlayerReference>();
			if (playerReference.PlayerReincarnation.isReincarnation)
			{
				Instantiate(reincarnationEffect, playerReference.transform.position, playerReference.transform.rotation);
				playerReference.effectUtils.HideDissolve(0.1f);
				playerReference.PlayerReincarnation.isReincarnation = false;
				Destroy(playerReference.PlayerReincarnation.reincarnationObj);
				playerReference.playerClasses.ChangeClass(data.classId, null, data.userId);
				RebuildSmearTargets(playerReference, null);
				playerReference.playerShooting?.HandleTransformationEvent();
				if (playerReference.IsLocalPlayer)
				{
					cooldownUI.instance.UpdateUiInformation();
					cooldownUI.instance.transformationEffectDirector.Play();
				}
			}
			playerReference.effectUtils.ShowDissolve(1f);
		}
	}

	[ServerRpc]
	public void AskForReincarnationServerRpc(NetworkObjectReference refObj)
	{
		if (refObj.TryGet(out NetworkObject playerNet))
		{
			PlayerReference playerReference = playerNet.GetComponent<PlayerReference>();

			if (playerReference.playerStatistics.playerStatData.health > 0f)
			{
				// Server-side Survivor modifiers: block as needed
				if (!playerReference.PlayerReincarnation.isReincarnation && SurvivorModifiers.spiritTransformBlocked)
				{
					Debug.Log("[Server] Transformation esprit bloquée par un événement.");
					return;
				}
				if (playerReference.PlayerReincarnation.isReincarnation && SurvivorModifiers.humanTransformBlocked)
				{
					Debug.Log("[Server] Retour humain bloqué par un événement.");
					return;
				}

				if (playerReference.PlayerReincarnation.isReincarnation)
				{
					DeTransformation(playerReference.playerStatistics.playerDataGame, playerReference);
				}
				else
				{
					if (playerReference.playerStatistics.ReincarnationPower >= 35f)
					{
						playerReference.playerStatistics.ReincarnationPower -= 35;
						Transformation(playerReference.playerStatistics.playerDataGame, playerReference);
					}
				}
			}
		}
	}

	void RebuildSmearTargets(PlayerReference reference, GameObject newController)
	{
		if (reference == null) return;
		reference.RecreateSmearEffect(newController != null ? newController.transform : null);
	}

}

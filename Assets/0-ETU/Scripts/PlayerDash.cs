using Lightbug.CharacterControllerPro.Implementation;
using Lightbug.CharacterControllerPro.Core;          // <- pour CharacterActor
using Lightbug.Utilities;                            // <- pour GetComponentInBranch (optionnel)
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Lightbug.CharacterControllerPro.Demo;
using RealToon.Script;

public class PlayerDash : NetworkBehaviour
{
	[Header("Dash")]
	public float dashSpeed = 12f;                     // = initialVelocity
	public float dashTime = 0.4f;                     // = duration
	public float dashCD = 0f;

	[Tooltip("Profil de vitesse du dash (0->1 temps normalisé). 1 = vitesse max, 0 = fin.")]
	public AnimationCurve movementCurve = AnimationCurve.Linear(0, 1, 1, 0);

	[Tooltip("Ignore les multiplicateurs de surface/volume Lightbug.")]
	public bool ignoreSpeedMultipliers = true;

	[Tooltip("Force le personnage en « not grounded » durant le dash (évite le frottement au sol).")]
	public bool forceNotGrounded = true;

	[Tooltip("Annule le dash si collision murale détectée.")]
	public bool cancelOnContact = true;

	private CharacterController controller;
	private PlayerClasses animator;
	private PlayerReference playerReference;
	private Slider dashUiObj;
	private SmearEffectHelper smearHelper;

	// Intégration Lightbug
	private MaterialController materialController;     // optionnel
	private float currentSpeedMultiplier = 1f;
	private Vector3 dashDirection = Vector3.forward;
	private float dashCursor = 0f;

	public bool isDashing;

	// Events to notify other systems (e.g., VFX) without polling
	public event System.Action OnDashStartEvent;
	public event System.Action OnDashEndEvent;

	private SmearEffectHelper GetSmearHelperCurrent()
	{
		if (playerReference != null && playerReference.smearEffectHelper != null)
		{
			smearHelper = playerReference.smearEffectHelper;
		}
		if (smearHelper == null)
		{
			smearHelper = GetComponent<SmearEffectHelper>();
		}
		return smearHelper;
	}

	public override void OnNetworkSpawn()
	{
		controller = GetComponent<CharacterController>();
		animator = GetComponent<PlayerClasses>();
		playerReference = GetComponent<PlayerReference>();
		smearHelper = GetSmearHelperCurrent();

		// Récupération optionnelle du MaterialController (pour speed multipliers)
		// Nécessite Lightbug.Utilities
		materialController = this.GetComponentInBranch<CharacterActor, MaterialController>();

		if (IsLocalPlayer)
		{
			InputManager.inputActions.Player.Jump.performed += JumpAction;
		}
	}

	public override void OnNetworkDespawn()
	{
		// Unsubscribe to avoid dangling callbacks across play sessions / despawn
		if (IsLocalPlayer)
		{
			try { InputManager.inputActions.Player.Jump.performed -= JumpAction; } catch { }
		}
		base.OnNetworkDespawn();
	}

	private void OnDisable()
	{
		// Also guard against Editor exit or disable order
		if (IsLocalPlayer)
		{
			try { InputManager.inputActions.Player.Jump.performed -= JumpAction; } catch { }
		}
	}

	private void DashActionCanceled(InputAction.CallbackContext context)
	{
		playerReference.characterBrain.characterActions.dash.value = false;
	}

	private void JumpAction(InputAction.CallbackContext context)
	{
		// Prevent playing jump animation if jump is blocked by a timeline
		if (playerReference != null && playerReference.playerMovement != null && playerReference.playerMovement.IsJumpBlocked())
			return;
		if (playerReference.playerStatistics.StunAirSeconds > 0f ||
			playerReference.playerStatistics.StunSeconds > 0f ||
			playerReference.playerStatistics.FreezeSeconds > 0f ||
			playerReference.playerStatistics.SleepSeconds > 0f ||
			playerReference.playerStatistics.ParaSeconds > 0f)
			return;

		if (playerReference.playerStatistics.ServerBlockSeconds > 0f)
			return;

		if (playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting)
			return;

		if (playerReference.playerStatistics.playerStatData.health <= 0f)
			return;

		if (playerReference.playerStatistics.isBehindWho)
			return;

		if (!playerReference.characterActor.IsGrounded)
			return;

		animator.animator.Play("jump", 0);
		animator.animator.Play("jump", 1);
		PlayAnimationServerRpc(gameObject, "jump");
	}

	public bool DashAction()
	{
		if (!IsLocalPlayer)
		{
			return false;
		}
		// Custom dash blocker (e.g., from spell timeline)
		if (playerReference != null && playerReference.playerMovement != null && playerReference.playerMovement.IsDashBlocked())
			return false;
		// Garde tes gardes-fous
		if (playerReference.playerStatistics.StunAirSeconds > 0f ||
			playerReference.playerStatistics.StunSeconds > 0f ||
			playerReference.playerStatistics.FreezeSeconds > 0f ||
			playerReference.playerStatistics.SleepSeconds > 0f ||
			playerReference.playerStatistics.ParaSeconds > 0f)
			return false;

		if (playerReference.playerStatistics.ServerBlockSeconds > 0f)
			return false;

		if (playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting ||
			playerReference.playerStatistics.playerStatData.health <= 0f)
			return false;

		if (playerReference.playerStatistics.isBehindWho)
			return false;

		// Déclenchement quand CD ok + pas de priorité de sort en cours
		if (animator.animator != null &&
			animator.animator.isInitialized &&
			!animator.animator.GetBool("spellPriority") &&
			dashCD <= 0f)
		{
			dashCD = 3f;
			StartCoroutine(Dash());
		}
		return true;
	}

	private void Update()
	{
		if (!IsLocalPlayer) return;

		if (dashCD > 0f)
		{
			dashCD -= Time.deltaTime;
			if (dashUiObj != null)
				dashUiObj.value -= Time.deltaTime;
		}
		else
		{
			if (dashUiObj != null)
			{
				playerReference.characterBrain.characterActions.dash.value = false;
				Destroy(dashUiObj.gameObject);
			}
		}
	}

	private IEnumerator Dash()
	{
		// --- Début : logique « push » inspirée de Lightbug.Demo.Dash ---

		isDashing = true;
		var smear = GetSmearHelperCurrent();
		if (smear != null)
		{
			smear.EnableSmear();
		}
		try { OnDashStartEvent?.Invoke(); } catch { }
		playerReference.characterBrain.characterActions.dash.value = true;

		// Animations (inchangé)
		animator.animator.Play("roulade", 0);
		animator.animator.Play("roulade", 1);
		PlayAnimationServerRpc(gameObject, "roulade");
		SoundManager.Instance.Play2D("dash");

		// Direction du dash : input si présent, sinon face forward
		var brain = playerReference.characterBrain;
		var actor = playerReference.characterActor;

		if (brain.characterActions.movement.value.sqrMagnitude > 0.01f)
		{
			// movement (x,y) => Right/Forward (Lightbug)
			dashDirection = (brain.characterActions.movement.value.y * actor.Forward +
							 brain.characterActions.movement.value.x * actor.Right).normalized;
		}
		else
		{
			dashDirection = actor.Forward;
		}

		// Multiplicateurs de vitesse selon surface/volume (facultatif)
		if (!ignoreSpeedMultipliers && materialController != null)
		{
			if (actor.IsGrounded)
				currentSpeedMultiplier = materialController.CurrentSurface.speedMultiplier *
										 materialController.CurrentVolume.speedMultiplier;
			else
				currentSpeedMultiplier = materialController.CurrentVolume.speedMultiplier;
		}
		else
		{
			currentSpeedMultiplier = 1f;
		}

		// Optionnel : désancrer du sol pour un dash « propre »
		bool resetAlwaysNotGrounded = false;
		if (forceNotGrounded && !actor.alwaysNotGrounded)
		{
			actor.alwaysNotGrounded = true;
			resetAlwaysNotGrounded = true;
		}

		actor.UseRootMotion = false; // pas de root motion pendant la poussée
		dashCursor = 0f;

		float elapsed = 0f;
		while (elapsed < dashTime)
		{
			// Annulation sur collision murale (comme cancelOnContact)
			if (cancelOnContact && actor.WallContacts.Count != 0)
				break;

			// Profil de vitesse via courbe
			float factor = movementCurve.Evaluate(dashCursor); // 0..1
			Vector3 dashVelocity = dashSpeed * currentSpeedMultiplier * factor * dashDirection;

			actor.Velocity = dashVelocity;

			// Avance du curseur de la courbe
			dashCursor += Time.deltaTime / dashTime;
			elapsed += Time.deltaTime;

			yield return null;
		}

		// Fin du dash : on relâche la contrainte et on laisse les autres états reprendre
		actor.Velocity = Vector3.zero;
		if (resetAlwaysNotGrounded)
			actor.alwaysNotGrounded = false;

		isDashing = false;
		smear = GetSmearHelperCurrent();
		if (smear != null)
		{
			smear.DisableSmear();
		}
		try { OnDashEndEvent?.Invoke(); } catch { }

		// --- Fin de la logique « push » ---

		// UI/CD (inchangé)
		if (dashUiObj == null)
		{
			Slider component = (Instantiate(Resources.Load("UI/bar/DashBar"), cooldownUI.instance.transform) as GameObject).GetComponent<Slider>();
			dashUiObj = component;
		}

		dashUiObj.maxValue = 3f;
		dashUiObj.value = 3f;
		dashCD = 3f;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayAnimationServerRpc(NetworkObjectReference casterObject, string animationName)
	{
		if (casterObject.TryGet(out NetworkObject casterNet))
		{
			GameObject caster = casterNet.gameObject;
			caster.GetComponent<PlayerClasses>().animator.Play(animationName);
			PlayAnimationClientRpc(caster, animationName);
		}
	}

	[ClientRpc]
	public void PlayAnimationClientRpc(NetworkObjectReference casterObject, string animationName)
	{
		if (casterObject.TryGet(out NetworkObject casterNet))
		{
			GameObject caster = casterNet.gameObject;
			caster.GetComponent<PlayerClasses>().animator.Play(animationName);
		}
	}
}

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Game;
using System;
using UnityEngine.AI;
using System.Collections;
using PhysicsBasedCharacterController;

public class SpellDamageTrigger : MonoBehaviour
{

	[SerializeField] private bool useTouchedRaycastAndNotTrigger;

	[SerializeField]
	private Spell spell;

	private Dictionary<ulong, float> touchedPlayers;

	private float timeDid;

	private PlayerReference casterReference;

	public Collider zoneCollider;

	// Utile pour dire quand il faut push et ï¿½ quel moment par rapport au sort.
	[HideInInspector] public float startedSpellTime;

	Vector3 GetRandomPositionInTrigger()
	{
		NavMeshHit hit;
		if (zoneCollider == null)
			Debug.Log("Il faut ajouter ï¿½ " + gameObject.name + " un BoxCollider pour SpellDamageTrigger");
		Vector3 randomPoint = new Vector3(
			UnityEngine.Random.Range(zoneCollider.bounds.min.x, zoneCollider.bounds.max.x),
			UnityEngine.Random.Range(zoneCollider.bounds.min.y, zoneCollider.bounds.max.y),
			UnityEngine.Random.Range(zoneCollider.bounds.min.z, zoneCollider.bounds.max.z)
		);
		if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
		{
			return hit.position;
		}
		else
		{
			return GetRandomPositionInTrigger();
		}

	}

#if !UNITY_SERVER

	private void Start()
	{
		if (!spell)
		{
			spell = GetComponent<Spell>();
		}
		zoneCollider = GetComponent<BoxCollider>();
		touchedPlayers = new Dictionary<ulong, float>();
		startedSpellTime = Time.time;

	}



	private void Update()
	{


		if (spell != null && spell.GetCaster() != null)
		{
			casterReference = spell.GetCaster().GetComponent<PlayerReference>();
		}

		if (casterReference != null && spell.cancelSpellIfStunned)
		{
			if (casterReference.playerStatistics.ServerBlockSeconds > 0f || casterReference.playerStatistics.StunAirSeconds > 0f || casterReference.playerStatistics.StunSeconds > 0f || casterReference.playerStatistics.FreezeSeconds > 0f || casterReference.playerStatistics.SleepSeconds > 0f || casterReference.playerStatistics.ParaSeconds > 0f)
			{
				Destroy(spell.gameObject);
			}
		}

		if (!useTouchedRaycastAndNotTrigger)
		{
			// Si le sort attaque qu'une fois, cela ne sert ï¿½ rien d'enlever le joueur
			if (spell.spellDamageOnce || !(Time.time > timeDid + spell.spellDamagePerSecond))
			{
				return;
			}
			timeDid = Time.time;
			foreach (KeyValuePair<ulong, float> item in touchedPlayers.ToList())
			{
				touchedPlayers.Remove(item.Key);
			}
		}


	}

	private void OnTriggerStay(Collider other)
	{
		if (other?.gameObject?.layer == 20)
		{
			SpellAntiMagic anti = other.GetComponent<SpellAntiMagic>();
			anti.ExternalDissipate(spell.gameObject);
			return;
		}
		if (!useTouchedRaycastAndNotTrigger)
		{
			if (spell.GetCaster() == null) return;
			if (spell.GetCaster() == other.gameObject)
				return;
			if (other.gameObject.layer != 3 && other.gameObject.layer != 7 && (spell.GetCaster().gameObject.layer == 9 || other.gameObject.layer != 9) && other.gameObject.layer != 12)
			{
				return;
			}
			PlayerReference otherRef = other.GetComponent<PlayerReference>();

			if ((otherRef.playerDash?.isDashing ?? false) || (otherRef.playerStatistics?.isInvincible ?? false))
				return;

			PlayerReference casterRef = spell.GetCaster().GetComponent<PlayerReference>();
			if (casterRef != null && otherRef != null && PlayerStatistics.AreAllies(casterRef, otherRef))
			{
				return;
			}
			if ((casterRef.playerStatistics.NotAttackMonsters && other.gameObject.layer == 7) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 3) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 9) || (casterRef.playerStatistics.NotAttackHeart && other.gameObject.layer == 12))
			{
				return;
			}

			if (otherRef.networkObject != null)
			{
				// Si le sort avale le joueur
				if (spell.keepTargetBehind != Vector3.zero)
				{
					otherRef.playerStatistics.isBehindWho = casterRef;
					otherRef.playerStatistics.isBehindVector = spell.keepTargetBehind;
					spell.targetBehind.Add(otherRef);
				}


				if (spell.pushTargetJumpSeconds > 0)
				{
					if (otherRef.playerShooting != null)
					{
						if (otherRef.playerShooting.nombreSortQuiMontent < 4)
						{
							otherRef.playerShooting.nombreSortQuiMontent += 1;
						}
					}
					else
					{
						Debug.Log(otherRef.gameObject.name + " a un playerShooting ï¿½ null, supprimer si c'est normal");
					}
				}

				ulong networkObjectId = otherRef.networkObject.NetworkObjectId;

				if (!touchedPlayers.ContainsKey(networkObjectId))
				{
					touchedPlayers.Add(networkObjectId, Time.time);

					otherRef.playerStatistics.SetAttacker(spell.GetCaster());
					if (otherRef.playerStatistics.playerStatData.health > 0f && spell.TargetPrefab != null)
					{
						spell.SpawnTargetPrefabIfAllowed(other.gameObject);
					}

					// SOUND MANAGER
					if (otherRef.IsLocalPlayer)
					{
						//SoundManager.instance.PlayAudioSfx(SoundManager.instance.hittedSound);
					}
					if (casterRef.IsLocalPlayer)
					{
						//SoundManager.instance.PlayAudioSfx(SoundManager.instance.hitSound);
						cooldownUI.instance.PlayCursorHitAnimation();
					}
					//

					SpellDamageParticle.CheckStatePlayer(otherRef, spell);
					if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !otherRef.playerClasses.isMonster)
					{
						CharacterManager manager = otherRef.GetComponent<CharacterManager>();
						if (manager != null)
						{
							manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
						}
					}
				}


				if (otherRef.gameObject.layer != 7 && otherRef.networkObject.IsLocalPlayer)
				{
					if (spell.pushTargetList != null && spell.pushTargetList.Count > 0)
					{
						foreach (PushType pushType in spell.pushTargetList)
						{
							// Vector3 direction, float startDelay, float endDelay, float pushPower, int index, PlayerClasses casterClass
							if (Time.time >= (startedSpellTime + pushType.pushStartDelay) && Time.time <= (startedSpellTime + pushType.pushEndDelay))
							{
								otherRef.playerShooting.PushTarget(otherRef, pushType);
							}

						}
					}
					if (spell.pushTargetBackSeconds > 0)
					{
						Vector3 moveDirection = casterRef.rigidBody.transform.position;
						otherRef.playerShooting.MoveTowards(other.gameObject, moveDirection, spell.pushTargetBackPower);
					}
					if (spell.pushTargetFrontSeconds > 0)
					{
						Vector3 moveDirection = casterRef.rigidBody.transform.position;
						otherRef.playerShooting.MoveTowards(other.gameObject, moveDirection, -spell.pushTargetFrontPower);
					}
				}
			}
		}
	}


	private void OnDestroy()
	{
		// On enlï¿½ve l'ï¿½tat d'avalement aux joueurs
		if (spell.targetBehind.Count > 0)
		{
			foreach (PlayerReference player in spell.targetBehind.ToList())
			{
				player.playerStatistics.isBehindWho = null;
				player.playerStatistics.isBehindVector = Vector3.zero;
			}
		}
		StopAllCoroutines();
	}

#endif


#if UNITY_SERVER
	private void Start()
	{
		if (!spell)
		{
			spell = GetComponent<Spell>();
		}
		touchedPlayers = new Dictionary<ulong, float>();
		Physics.IgnoreLayerCollision(0, 2);

		startedSpellTime = Time.time;
	}

	private void Update()
	{

		if (spell.spellDamageOnce || !(Time.time > timeDid + spell.spellDamagePerSecond))
		{
			return;
		}
		timeDid = Time.time;
		foreach (KeyValuePair<ulong, float> item in touchedPlayers.ToList())
		{
			touchedPlayers.Remove(item.Key);
		}

		if (spell != null)
		{
			casterReference = spell.GetCaster()?.GetComponent<PlayerReference>();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!useTouchedRaycastAndNotTrigger)
		{
			if (spell.GetCaster() == other.gameObject || spell.GetCaster() == null) return;
			if (other.gameObject.layer != 3 && other.gameObject.layer != 7 && (spell.GetCaster().gameObject.layer == 9 || other.gameObject.layer != 9) && other.gameObject.layer != 12)
			{
				return;
			}
			PlayerReference otherRef = other.GetComponent<PlayerReference>();
			if ((otherRef.playerDash?.isDashing ?? false) || (otherRef.playerStatistics?.isInvincible ?? false)) return;

			if (spell.GetCaster() != null)
			{
				PlayerReference casterRef = spell.GetCaster().GetComponent<PlayerReference>();
				if (casterRef != null && otherRef != null && PlayerStatistics.AreAllies(casterRef, otherRef))
				{
					return;
				}
				if ((casterRef.playerStatistics.NotAttackMonsters && other.gameObject.layer == 7) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 3) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 9) || (casterRef.playerStatistics.NotAttackHeart && other.gameObject.layer == 12))
				{
					return;
				}
				if (otherRef.networkObject != null)
				{
					ulong networkObjectId = otherRef.networkObject.NetworkObjectId;

					if (spell.keepTargetBehind != Vector3.zero)
					{
						otherRef.playerStatistics.isBehindWho = casterRef;
						otherRef.playerStatistics.isBehindVector = spell.keepTargetBehind;
						spell.targetBehind.Add(otherRef);
					}

					if (!touchedPlayers.ContainsKey(networkObjectId))
					{
						touchedPlayers.Add(networkObjectId, Time.time);

						otherRef.playerStatistics.SetAttacker(spell.GetCaster());
						if (otherRef.playerStatistics.playerStatData.health > 0f && spell.TargetPrefab != null)
						{
							spell.SpawnTargetPrefabIfAllowed(other.gameObject);
						}




						if (otherRef.follow != null)
						{
							float pushBonus = 0;

							// Gestion des push sur le monstre en utilisant pushTargetList
							if (spell.pushTargetList != null && spell.pushTargetList.Count > 0)
							{
								foreach (PushType pushType in spell.pushTargetList)
								{
									float elapsedSpellTime = Time.time - startedSpellTime;

									if (elapsedSpellTime <= pushType.pushEndDelay)
									{
										float remainingPushDuration = pushType.pushEndDelay - Mathf.Max(pushType.pushStartDelay, elapsedSpellTime);

										if (remainingPushDuration > 0)
										{
											Vector3 pushDirection = transform.rotation * pushType.pushPower;
											otherRef.follow.TriggerPush(pushDirection, 0.2f);
										}
									}
								}
							}

							// ----- NEW: gestion pushTargetBack/front pour les monstres -----
							if (casterRef != null)
							{
								// Knockback (eloigner du caster)
								if (spell.pushTargetBackSeconds > 0f && MathF.Abs(spell.pushTargetBackPower) > 0f)
								{
									otherRef.follow.MoveTowardsFromPoint(
									 casterRef.rigidBody != null ? casterRef.rigidBody.transform.position : casterRef.transform.position,
									 spell.pushTargetBackPower,
									 spell.pushTargetBackSeconds
									);
								}

								// Knockforward (se rapprocher du caster)
								if (spell.pushTargetFrontSeconds > 0f && MathF.Abs(spell.pushTargetFrontPower) > 0f)
								{
									// On passe une power negative pour aller VERS le point (voir MoveTowardsFromPoint)
									otherRef.follow.MoveTowardsFromPoint(
									 casterRef.rigidBody != null ? casterRef.rigidBody.transform.position : casterRef.transform.position,
									 -spell.pushTargetFrontPower,
									 spell.pushTargetFrontSeconds
									);
								}
							}
						}

						// Gestion des stats / donnees
						SpellDamageParticle.CheckStatePlayer(otherRef, spell);
						if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !otherRef.playerClasses.isMonster)
						{
							CharacterManager manager = otherRef.GetComponent<CharacterManager>();
							if (manager != null)
							{
								manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
							}
						}

						// Gestion du spawn d'ï¿½lï¿½ments:
						foreach (SpawnType spawnItem in spell.spawnThisOnCollision)
						{
							if (spawnItem.isNetworkObject)
							{
								spawnItem.spawnObject = Instantiate(spawnItem.spawnObject, other.gameObject.transform.position, Quaternion.identity);
								spawnItem.spawnObject.GetComponent<NetworkObject>().Spawn();
								spell.spawnedObjects.Add(spawnItem);
							}
						}
					}
				}
			}
		}
	}

	private void OnDestroy()
	{
		// On enlï¿½ve l'ï¿½tat d'avalement aux joueurs
		if (spell.targetBehind.Count > 0)
		{
			foreach (PlayerReference player in spell.targetBehind.ToList())
			{
				player.playerStatistics.isBehindWho = null;
				player.follow.GetAgent().enabled = true;
			}
		}
		StopAllCoroutines();
	}


#endif


	// Cela concerne les projectiles uniquement (qui fonctionne avec ProjectileController).
	public void Touched(Collider other)
	{
		if (spell.GetCaster() == other.gameObject)
			return;
		if (other.gameObject.layer != 3 && other.gameObject.layer != 7 && (spell.GetCaster().gameObject.layer == 9 || other.gameObject.layer != 9) && other.gameObject.layer != 12)
		{
			return;
		}
		PlayerReference otherRef = other.GetComponent<PlayerReference>();
		if ((otherRef.playerDash?.isDashing ?? false) || (otherRef.playerStatistics?.isInvincible ?? false))
			return;
		PlayerReference casterRef = spell.GetCaster().GetComponent<PlayerReference>();
		if (casterRef != null && otherRef != null && PlayerStatistics.AreAllies(casterRef, otherRef))
		{
			return;
		}
		if ((casterRef.playerStatistics.NotAttackMonsters && other.gameObject.layer == 7) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 3) || (casterRef.playerStatistics.NotAttackPlayers && other.gameObject.layer == 9) || (casterRef.playerStatistics.NotAttackHeart && other.gameObject.layer == 12))
		{
			return;
		}
		if (otherRef.networkObject != null)
		{
			ulong networkObjectId = otherRef.networkObject.NetworkObjectId;
			if (!touchedPlayers.ContainsKey(networkObjectId))
			{
				touchedPlayers.Add(networkObjectId, Time.time);

				// Nous devons pousser le joueur que chez lui.
				// TO DO: Sï¿½curitï¿½ pour vï¿½rifier que le joueur se fait bien pousser.



				if (otherRef.gameObject.layer != 7 && otherRef.networkObject.IsLocalPlayer)
				{
					if (spell.pushTargetList != null && spell.pushTargetList.Count > 0)
					{
						foreach (PushType pushType in spell.pushTargetList)
						{
							// Vector3 direction, float startDelay, float endDelay, float pushPower, int index, PlayerClasses casterClass
							if (Time.time >= (startedSpellTime + pushType.pushStartDelay) && Time.time <= (startedSpellTime + pushType.pushEndDelay))
							{
								otherRef.playerShooting.PushTarget(otherRef, pushType);
							}

						}
					}

					if (spell.pushTargetBackSeconds > 0)
					{
						Vector3 moveDirection = casterRef.rigidBody.transform.position;
						otherRef.playerShooting.MoveTowards(other.gameObject, moveDirection, spell.pushTargetBackPower);
					}
				}

				otherRef.playerStatistics.SetAttacker(spell.GetCaster());
				if (otherRef.playerStatistics.playerStatData.health > 0f && spell.TargetPrefab != null)
				{
					spell.SpawnTargetPrefabIfAllowed(other.gameObject);
				}
				SpellDamageParticle.CheckStatePlayer(otherRef, spell);
				if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !otherRef.playerClasses.isMonster)
				{
					CharacterManager manager = otherRef.GetComponent<CharacterManager>();
					if (manager != null)
					{
						manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
					}
				}
				// SOUND MANAGER
				if (otherRef.IsLocalPlayer)
				{
					// SoundManager.instance.PlayAudioSfx(SoundManager.instance.hittedSound);
				}
				if (casterRef.IsLocalPlayer)
				{
					// SoundManager.instance.PlayAudioSfx(SoundManager.instance.hitSound);
					cooldownUI.instance.PlayCursorHitAnimation();
				}
				//

				// Gestion du spawn d'ï¿½lï¿½ments:
				foreach (SpawnType spawnItem in spell.spawnThisOnCollision)
				{
					// S'il s'agit d'un objet ï¿½ afficher juste pour les clients
					if (!spawnItem.isNetworkObject)
					{
						spawnItem.spawnObject = Instantiate(spawnItem.spawnObject, other.gameObject.transform.position, Quaternion.identity);
						spell.spawnedObjects.Add(spawnItem);
					}
				}
			}
		}
	}



}



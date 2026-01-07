using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.SceneManagement;
using Game;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using PhysicsBasedCharacterController;
using System;

public class SpellDamageParticle : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] private Spell spell;

    private Dictionary<ulong, float> touchedPlayers;

    float timeDid;

    // Utile pour dire quand il faut push et ï¿½ quel moment par rapport au sort.
    [HideInInspector] public float startedSpellTime;

    private void Awake()
    {
        if (spell == null) spell = GetComponent<Spell>();
        touchedPlayers = new Dictionary<ulong, float>();
    }

#if !UNITY_SERVER
    private void Start()
    {
        startedSpellTime = Time.time;
    }
#endif
    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        // Pour chaque joueur de la liste...
        // Si le Time.time > value + spell.damageEachSeconds...
        // ...Supprimer la clï¿½.
        if (spell != null)
        {
            if (!spell.spellDamageOnce)
            {
                if (Time.time > timeDid + spell.spellDamagePerSecond)
                {
                    timeDid = Time.time;
                    foreach (var item in touchedPlayers.ToList())
                    {
                        touchedPlayers.Remove(item.Key);
                    }

                }
            }
        }
    }

#if !UNITY_SERVER
    private void OnParticleCollision(GameObject other)
    {
        if (other?.gameObject?.layer == 20)
        {
            SpellAntiMagic anti = other.GetComponent<Game.SpellAntiMagic>();
            anti.ExternalDissipate(spell.gameObject);
            return;
        }
        if (other?.gameObject?.layer != 3 && other?.gameObject?.layer != 7 && (spell.GetCaster()?.gameObject?.layer == 9 || other?.gameObject?.layer != 9) && other?.gameObject?.layer != 12)
        {
            return;
        }
        if (spell == null && spell.GetCaster() == null)
            return;
        if (spell.GetCaster() != other.gameObject)
        {
            PlayerReference casterRef = spell.casterRef;


            PlayerReference otherReference = other.GetComponent<PlayerReference>();

            if ((otherReference.playerDash?.isDashing ?? false) || (otherReference.playerStatistics?.isInvincible ?? false))
                return;
            if (casterRef != null && otherReference != null && PlayerStatistics.AreAllies(casterRef, otherReference))
                return;
            // Si nous n'avons pas ï¿½ attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
            if (casterRef.playerStatistics.NotAttackMonsters && other.layer == 7)
                return;
            if (casterRef.playerStatistics.NotAttackPlayers && other.layer == 3)
                return;
            if (casterRef.playerStatistics.NotAttackPlayers && other.layer == 9)
                return;
            if (casterRef.playerStatistics.NotAttackHeart && other.layer == 12)
                return;
            if (spell.spellDamageOnce)
            {
                Debug.Log(spell.gameObject.name);
                NetworkObject netOther = other.GetComponent<NetworkObject>();
                // Et que le joueur n'est pas dï¿½jï¿½ dans la liste des objets dï¿½jï¿½ touchï¿½s.
                if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                {
                    // On l'ajoute des les objets touchï¿½s
                    touchedPlayers.Add(netOther.NetworkObjectId, Time.time);

                    if (otherReference.gameObject.layer != 7 && otherReference.networkObject.IsLocalPlayer)
                    {
                        if (spell.pushTargetList != null && spell.pushTargetList.Count > 0)
                        {
                            foreach (PushType pushType in spell.pushTargetList)
                            {
                                // Vector3 direction, float startDelay, float endDelay, float pushPower, int index, PlayerClasses casterClass
                                if (Time.time >= (startedSpellTime + pushType.pushStartDelay) && Time.time <= (startedSpellTime + pushType.pushEndDelay))
                                {
                                    Debug.Log("PUSH TARGET with  " + spell.gameObject.name);
                                    otherReference.playerShooting.PushTarget(otherReference, pushType);
                                }

                            }
                        }
                        if (spell.pushTargetBackSeconds > 0)
                        {
                            Vector3 moveDirection = casterRef.rigidBody.transform.position;
                            otherReference.playerShooting.MoveTowards(other.gameObject, moveDirection, spell.pushTargetBackPower);
                        }
                        if (spell.pushTargetFrontSeconds > 0)
                        {
                            Vector3 moveDirection = casterRef.rigidBody.transform.position;
                            otherReference.playerShooting.MoveTowards(other.gameObject, moveDirection, -spell.pushTargetFrontPower);
                        }
                    }

                    if (otherReference.IsLocalPlayer)
                    {
                        // SoundManager.instance.PlayAudioSfx(SoundManager.instance.hittedSound);
                    }
                    if (casterRef.IsLocalPlayer)
                    {
                        // SoundManager.instance.PlayAudioSfx(SoundManager.instance.hitSound);
                    }
                    CheckStatePlayer(otherReference, spell);

                    if (otherReference.playerStatistics.playerStatData.health > 0f)
                    {
                        spell.SpawnTargetPrefabIfAllowed(otherReference.gameObject);
                    }
                }
            }
            else
            {
                NetworkObject netOther = other.GetComponent<NetworkObject>();
                // Et que le joueur n'est pas dï¿½jï¿½ dans la liste des objets dï¿½jï¿½ touchï¿½s.
                if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                {
                    // On rï¿½cupï¿½re et on lui applique les dï¿½gï¿½ts, effets, etc.
                    otherReference.playerStatistics.SetAttacker(spell.GetCaster());

                    CheckStatePlayer(otherReference, spell);
                    if (otherReference.playerStatistics.playerStatData.health > 0f)
                    {
                        spell.SpawnTargetPrefabIfAllowed(otherReference.gameObject);
                    }
                }
            }
        }
    }
#endif


#if UNITY_SERVER

    private void OnParticleCollision(GameObject other)
    {
        if (spell == null) return;
        if (spell.GetCaster() != other.gameObject)
        {

            // Si on percute un anti-magie cÃ´tÃ© serveur, on dÃ©lÃ¨gue au composant AntiMagic serveur.
            if (other.layer == 20)
            {
                SpellAntiMagic anti = other.GetComponent<Game.SpellAntiMagic>();
                anti.ExternalDissipate(spell.gameObject);
                return;
            }

            if (other.layer == 3 || other.layer == 7 || (spell.casterRef.playerClasses.isMonster && other.layer == 9) || other.layer == 12)
            {
                PlayerReference stats = other.GetComponent<PlayerReference>();
                PlayerReference statsp = spell.casterRef;
                if ((stats.playerDash?.isDashing ?? false) || (stats.playerStatistics?.isInvincible ?? false)) return;
                if (statsp != null && stats != null && PlayerStatistics.AreAllies(statsp, stats)) return;

                // Si nous n'avons pas ï¿½ attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
                if (statsp.playerStatistics.NotAttackMonsters && other.layer == 7) return;
                if (statsp.playerStatistics.NotAttackPlayers && other.layer == 3) return;
                if (statsp.playerStatistics.NotAttackPlayers && other.layer == 9) return;
                if (statsp.playerStatistics.NotAttackHeart && other.layer == 12) return;
                // Si on doit taper le joueur adverse une seule fois.
                NetworkObject netOther = other.GetComponent<NetworkObject>();


                if (spell.spellDamageOnce)
                {
                    // Et que le joueur n'est pas dï¿½jï¿½ dans la liste des objets dï¿½jï¿½ touchï¿½s.
                    if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                    {
                        // On l'ajoute des les objets touchï¿½s
                        touchedPlayers.Add(netOther.NetworkObjectId, Time.time);

                        // On rï¿½cupï¿½re et on lui applique les dï¿½gï¿½ts, effets, etc.
                        stats.playerStatistics.SetAttacker(spell.GetCaster());

                        CheckStatePlayer(stats, spell);

                    }
                }
                // Si on tape un joueur qu'on peut taper plusieurs fois
                else
                {
                    // Et que le joueur n'est pas dï¿½jï¿½ dans la liste des objets dï¿½jï¿½ touchï¿½s.
                    if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                    {
                        // On rï¿½cupï¿½re et on lui applique les dï¿½gï¿½ts, effets, etc.
                        stats.playerStatistics.SetAttacker(spell.GetCaster());
                        CheckStatePlayer(stats, spell);

                    }
                }

                if (stats.follow != null)
                {
                    PlayerReference casterRef = statsp;
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
                                    stats.follow.TriggerPush(pushDirection, 0.2f);
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
                            stats.follow.MoveTowardsFromPoint(
                                casterRef.rigidBody != null ? casterRef.rigidBody.transform.position : casterRef.transform.position,
                                spell.pushTargetBackPower,
                                spell.pushTargetBackSeconds
                            );
                        }

                        // Knockforward (se rapprocher du caster)
                        if (spell.pushTargetFrontSeconds > 0f && MathF.Abs(spell.pushTargetFrontPower) > 0f)
                        {
                            // On passe une power negative pour aller VERS le point (voir MoveTowardsFromPoint)
                            stats.follow.MoveTowardsFromPoint(
                                casterRef.rigidBody != null ? casterRef.rigidBody.transform.position : casterRef.transform.position,
                                -spell.pushTargetFrontPower,
                                spell.pushTargetFrontSeconds
                            );
                        }
                    }
                }
            }
        }
    }

#endif

    public static void CheckStatePlayer(PlayerReference playerRef, Spell spell)
    {
        // Si on est mort, pas besoin de continuer et voir les effets du joueur.
        if (playerRef.playerStatistics.playerStatData.health <= 0)
            return;

#if UNITY_SERVER


        bool isHealActive = spell != null && spell.healTargets != Game.Spell.HealTargets.None && spell.healValue > 0f;
        if (isHealActive)
        {
            // Heal branch
            if (ShouldHealTarget(playerRef, spell))
            {
                playerRef.playerStatistics.ApplyHeal(playerRef.gameObject, spell.healValue, spell.casterRef);
            }
        }
        else
        {
            if (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer)
            {
                playerRef.playerStatistics.ApplyDamage(playerRef.gameObject, spell.spellDamage, spell.casterRef);
            }
            else
            {
                playerRef.playerStatistics.ApplyDamage(playerRef.gameObject, spell.spellDamage + (spell.casterRef.playerStatistics.playerStatData.soul * 0.2f), spell.casterRef);
            }
        }
#else
        if (playerRef.playerStatistics.playerStatData.health > 0f)
        {
            spell.SpawnTargetPrefabIfAllowed(playerRef.gameObject);
        }
#endif
        bool survivorMode = Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor;
        bool streamerMode = Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer;
        bool isHealActive2 = spell != null && spell.healTargets != Game.Spell.HealTargets.None && spell.healValue > 0f;

        // Survivor: monsters should not stun players. If caster is monster, nullify CC in Survivor.
        PlayerReference casterRef = spell?.casterRef;
        bool casterIsMonster = casterRef != null && casterRef.playerClasses != null && casterRef.playerClasses.isMonster;

        // Survivor bonus: +20% stun/CC duration per stack (multiplicative)
        float stunFactor = 1f;
        // In Survivor, monsters should not stun players
        bool allowCrowdControl = !(survivorMode && casterIsMonster);

        if (!isHealActive2 && allowCrowdControl && spell.StunSeconds > 0 && playerRef.playerStatistics.StunImmunitySeconds <= 0f)
        {
            float v = spell.StunSeconds * Mathf.Max(0f, stunFactor);
            if (v > 0f)
            {
                playerRef.playerStatistics.StunSeconds = v;
                if (!playerRef.playerClasses.isMonster) playerRef.characterBrain.inputHandlerSettings.InputHandler.isStunned = true;
            }
        }
        if (!isHealActive2 && allowCrowdControl && spell.StunAirSeconds > 0 && playerRef.playerStatistics.StunImmunitySeconds <= 0f)
        {
            float v = spell.StunAirSeconds * Mathf.Max(0f, stunFactor);
            if (v > 0f)
            {
                playerRef.playerStatistics.StunAirSeconds = v;
                if (!playerRef.playerClasses.isMonster) playerRef.characterBrain.inputHandlerSettings.InputHandler.isStunnedAir = true;
            }
        }
        if (!isHealActive2 && allowCrowdControl && spell.FreezeSeconds > 0 && playerRef.playerStatistics.StunImmunitySeconds <= 0f)
        {
            float v = spell.FreezeSeconds * Mathf.Max(0f, stunFactor);
            if (v > 0f)
            {
                playerRef.playerStatistics.FreezeSeconds = v;
                if (!playerRef.playerClasses.isMonster) playerRef.characterBrain.inputHandlerSettings.InputHandler.isFrozen = true;
            }
        }
        if (!isHealActive2 && allowCrowdControl && spell.ParaSeconds > 0 && playerRef.playerStatistics.StunImmunitySeconds <= 0f)
        {
            float v = spell.ParaSeconds * Mathf.Max(0f, stunFactor);
            if (v > 0f)
            {
                playerRef.playerStatistics.ParaSeconds = v;
                if (!playerRef.playerClasses.isMonster) playerRef.characterBrain.inputHandlerSettings.InputHandler.isParalyzed = true;
            }
        }
        if (!isHealActive2 && allowCrowdControl && spell.SleepSeconds > 0 && playerRef.playerStatistics.StunImmunitySeconds <= 0f)
        {
            float v = spell.SleepSeconds * Mathf.Max(0f, stunFactor);
            if (v > 0f)
            {
                playerRef.playerStatistics.SleepSeconds = v;
                if (!playerRef.playerClasses.isMonster) playerRef.characterBrain.inputHandlerSettings.InputHandler.isSleeping = true;
            }
        }

        if (!isHealActive2 && spell.movementSpeedFactor > 0 && spell.movementSpeedFactor != 1)
        {
            if (playerRef.playerMovement != null)
            {
                playerRef.playerMovement.movementSpeedFactors.Add(new List<float>() { spell.movementSpeedFactor, spell.movementSpeedSeconds, Time.time });
            }
            else if (playerRef.follow != null)
            {
                playerRef.follow.movementSpeedFactors.Add(new List<float>() { spell.movementSpeedFactor, spell.movementSpeedSeconds, Time.time });
            }
        }
        if (!isHealActive2 && spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !playerRef.playerClasses.isMonster)
        {
            CharacterManager manager = playerRef.GetComponent<CharacterManager>();
            if (manager != null)
            {
                manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
            }
        }

#if UNITY_SERVER
        if (!isHealActive2 && (playerRef.playerStatistics.StunSeconds > 0f ||
            playerRef.playerStatistics.FreezeSeconds > 0f ||
            playerRef.playerStatistics.SleepSeconds > 0f ||
            playerRef.playerStatistics.ParaSeconds > 0f ||
            playerRef.playerStatistics.StunAirSeconds > 0f))
        {
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { playerRef.OwnerClientId }
                }
            };

            playerRef.playerStatistics.ApplyStunClientRpc(
                playerRef.playerStatistics.StunSeconds,
                playerRef.playerStatistics.FreezeSeconds,
                playerRef.playerStatistics.SleepSeconds,
                playerRef.playerStatistics.ParaSeconds,
                playerRef.playerStatistics.StunAirSeconds,
                rpcParams);
        }
#endif
    }

    private static bool ShouldHealTarget(PlayerReference targetRef, Spell spell)
    {
        if (spell == null || targetRef == null) return false;
        var casterRef = spell.casterRef;
        if (casterRef == null) return false;

        bool isSelf = targetRef == casterRef;
        bool sameGroup = PlayerStatistics.AreAllies(targetRef, casterRef);

        switch (spell.healTargets)
        {
            case Game.Spell.HealTargets.AllyOnly:
                return sameGroup && !isSelf;
            case Game.Spell.HealTargets.AllyOrEnemy:
                return true;
            case Game.Spell.HealTargets.SelfOnly:
                return isSelf;
            case Game.Spell.HealTargets.SelfAndAlly:
                return isSelf || sameGroup;
            default:
                return false;
        }
    }

}





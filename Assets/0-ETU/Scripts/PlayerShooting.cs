using Cinemachine;
using Game;
using Lightbug.CharacterControllerPro.Core;
using Lightbug.CharacterControllerPro.Implementation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RealToon.Script;

public class PlayerShooting : NetworkBehaviour
{

    public Transform castProjectilePosition;
    public Transform castNormalPosition;

    public Camera cam;
    //[HideInInspector]public CinemachineVirtualCamera virtualCamera;

    public Ray RayMouse;
    public Vector3 direction;
    private Quaternion rotation;
    public Vector3 offsetProjectileSmall;


    public NetworkVariable<Quaternion> directionY = new NetworkVariable<Quaternion>(default, default, NetworkVariableWritePermission.Owner);


    // Variable permettant de donner l'endroit ou vont aller les projectiles
    [SerializeField] float verticalOffset = 0.5f;

    [SerializeField] public bool isFastCast;

    private GameObject aoeZone;

    // Tout le monde sauf le localPlayer
    int layerMask = ~((1 << 0) | (1 << 9) | (1 << 5) | (1 << 10) | (1 << 11) | (1 << 2));
    //    int layerMask = (1 << 3) | (1 << 6) | (1 << 7);

    public int isPreparingAoe = -1;

    // NetworkVariable<bool> shooting = new NetworkVariable<bool>(false);
    // bool shooting = false;
    /*    float shootTimer = 0f;
        float shootRate = 5f;*/

    PlayerClasses classInfo;
    PlayerMovement mouvementJoueur;
    PlayerStatistics playerStats;

    PlayerReference playerReference;

    private Spell[] RealSpells =>
        playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation
            ? classInfo.reincarnationRealSpells
            : classInfo.humanRealSpells;

    // Track caster-control timelines per spell slot (started from this component)
    private Coroutine[] casterControlCoroutines = new Coroutine[4];
    private Coroutine currentCastRoutine;
    private Slider castUiObj;
    private Spell currentCastingSpell;
    private int currentCastingIndex = -1;
    private GameObject currentCastVfx;
    private string lastCastCancelReason;
    private bool castCancelRequestedByOtherSpell;
    private bool castCancelRequestedByTransformation;
    private bool castCancelRequestedByAutoAttack;
    private bool wasHitDuringCast;
    private bool wasPushedDuringCast;
    private readonly List<Coroutine> activePushCoroutines = new List<Coroutine>();

    bool reActivateAgent = false;


    bool respectHitDistance = false;
    int nombreQuiFaitFiger = 0;


    public int nombreSortQuiMontent = 0;

    public float distanceToPreventHit = 15f;

    /// <summary>
    /// Variable qui garde le dernier temps ou nous avons enlev? le nombre de sort qui font monter.
    /// </summary>
    public float lastTimeMonteSuppression = 0;

    private float stunAccumulated;



    private List<GameObject> statesPrefab;

    public float multiplierUp = 4f;


    public override void OnNetworkDespawn()
    {
        directionY.OnValueChanged -= OnStateChanged;

        if (!classInfo.isMonster && IsClient && IsLocalPlayer)
        {
            try { InputManager.inputActions.Player.Spell1.performed -= Spell1Action; } catch { }
            try { InputManager.inputActions.Player.Spell2.performed -= Spell2Action; } catch { }
            try { InputManager.inputActions.Player.Spell3.performed -= Spell3Action; } catch { }
            try { InputManager.inputActions.Player.Spell4.performed -= Spell4Action; } catch { }
        }

        if (playerStats != null)
        {
            try { playerStats.OnLocalHealthChanged -= OnLocalHealthChangedDuringCast; } catch { }
        }
    }


    public void OnStateChanged(Quaternion previous, Quaternion current)
    {
        // note: `State.Value` will be equal to `current` here
        if (directionY.Value != castProjectilePosition.rotation)
        {
            castProjectilePosition.rotation = directionY.Value;
        }
    }


    private void Awake()
    {
        classInfo = GetComponent<PlayerClasses>();
        mouvementJoueur = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStatistics>();
        playerReference = GetComponent<PlayerReference>();
        statesPrefab = new List<GameObject>() { null, null, null, null, null };
        if (!classInfo.isMonster) cooldownUI.instance.ChangeStateMsg(false);
    }

    public override void OnNetworkSpawn()
    {
        // Pour changer la r?p?tition de lanc?e de projectile.
        /*        em = projectile.emission;
                em.burstCount = 0;*/
        if (classInfo.isMonster)
        {
            directionY.OnValueChanged += OnStateChanged;
        }

#if !UNITY_SERVER
        isFastCast = PlayerPrefs.GetInt("fastCast") == 1 ? true : false;
#endif

        if (playerStats != null)
        {
            playerStats.OnLocalHealthChanged += OnLocalHealthChangedDuringCast;
        }

        if (!classInfo.isMonster && IsClient && IsLocalPlayer)
        {
            InputManager.inputActions.Player.Spell1.performed += Spell1Action;
            InputManager.inputActions.Player.Spell2.performed += Spell2Action;
            InputManager.inputActions.Player.Spell3.performed += Spell3Action;
            InputManager.inputActions.Player.Spell4.performed += Spell4Action;
        }
    }

    private void OnDisable()
    {
        if (!classInfo.isMonster && IsClient && IsLocalPlayer)
        {
            try { InputManager.inputActions.Player.Spell1.performed -= Spell1Action; } catch { }
            try { InputManager.inputActions.Player.Spell2.performed -= Spell2Action; } catch { }
            try { InputManager.inputActions.Player.Spell3.performed -= Spell3Action; } catch { }
            try { InputManager.inputActions.Player.Spell4.performed -= Spell4Action; } catch { }
        }
    }

    public void SetFastCast(int newValue)
    {
        isFastCast = newValue == 1;
    }

    public void StartIt()
    {
        cooldownUI.instance.UpdateIt();
    }

    private void Spell4Action(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        // Si on nous bloque les sorts
        if (!enabled)
            return;


        ExecuteSpell(3, playerReference.PlayerReincarnation.IsReincarnation ? classInfo.spiritSpell4CD : classInfo.warriorSpell4CD);
    }

    private void Spell3Action(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        // Si on nous bloque les sorts
        if (!enabled)
            return;
        ExecuteSpell(2, playerReference.PlayerReincarnation.IsReincarnation ? classInfo.spiritSpell3CD : classInfo.warriorSpell3CD);
    }

    private void Spell2Action(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        // Si on nous bloque les sorts
        if (!enabled)
            return;
        ExecuteSpell(1, playerReference.PlayerReincarnation.IsReincarnation ? classInfo.spiritSpell2CD : classInfo.warriorSpell2CD);
    }

    private void Spell1Action(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        // Si on nous bloque les sorts
        if (!enabled)
            return;
        ExecuteSpell(0, playerReference.PlayerReincarnation.IsReincarnation ? classInfo.spiritSpell1CD : classInfo.warriorSpell1CD);
    }

    void Update()
    {
        if (cam != null)
        {
            RaycastHit hit;
            if (IsLocalPlayer)
            {
                if (!classInfo.isMonster)
                {
                    // Convertit cette position en coordonn?es du monde
                    Vector3 screenCenter = new Vector3(Screen.width / 2, (Screen.height / 2) + Screen.height * verticalOffset, 0);
                    RayMouse = cam.ScreenPointToRay(screenCenter);
                    if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out hit, Mathf.Infinity, layerMask))
                    {
                        float distanceToHit = Vector3.Distance(transform.position, hit.point);

                        // Si la distance est trop petite, prend un point plus ?loign?
                        if (distanceToHit < distanceToPreventHit)
                        {
                            Vector3 farPoint = RayMouse.origin + RayMouse.direction.normalized * 100;
                            RotateToMouseDirection(farPoint + offsetProjectileSmall);
                        }
                        else
                        {
                            RotateToMouseDirection(hit.point);
                        }
                    }
                    else
                    {
                        Vector3 farPoint = RayMouse.origin + RayMouse.direction.normalized * 100;
                        RotateToMouseDirection(farPoint);
                    }
                }
            }


        }
        // R?gulation de la gravit? (sorts qui nous font monter)

        // A REFAIRE

        /*        if (nombreSortQuiMontent > 0 && Time.time > (lastTimeMonteSuppression + 0.2f)) {
                    lastTimeMonteSuppression = Time.time;
                    playerReference.playerShooting.nombreSortQuiMontent -= 1;

                    // playerReference.playerMovement.gravity = 0;
                }
                if (nombreSortQuiMontent == 0 && playerReference.playerMovement.gravity >= 0) {
                    playerReference.playerMovement.gravity = -25;
                }
                if (nombreSortQuiMontent < 0) {
                    nombreSortQuiMontent = 0;
                    playerReference.playerMovement.gravity = -25;
                }*/

        if (IsLocalPlayer)
        {
            if (playerReference.playerStatistics.ServerBlockSeconds > 0f || playerReference.playerStatistics.StunAirSeconds > 0f || playerReference.playerStatistics.StunSeconds > 0f || playerReference.playerStatistics.FreezeSeconds > 0f || playerReference.playerStatistics.SleepSeconds > 0f || playerReference.playerStatistics.ParaSeconds > 0f)
            {

                double value = Math.Round(Mathf.Max(playerReference.playerStatistics.StunAirSeconds, playerReference.playerStatistics.StunSeconds, playerReference.playerStatistics.FreezeSeconds, playerReference.playerStatistics.SleepSeconds, playerReference.playerStatistics.ParaSeconds), 1, MidpointRounding.AwayFromZero);

                if (value > 0)
                {
                    cooldownUI.instance.SetStateMsg(value);
                    cooldownUI.instance.ChangeStateMsg(true);
                }
                playerReference.characterBrain.characterActions.movement.value = Vector2.zero;
            }
            else
            {
                cooldownUI.instance.ChangeStateMsg(false);
            }
        }
        if (classInfo.animator != null && classInfo.animator.isInitialized)
        {
            if (playerStats.ServerBlockSeconds > 0f || playerReference.playerStatistics.StunAirSeconds > 0f || playerReference.playerStatistics.StunSeconds > 0f || playerReference.playerStatistics.FreezeSeconds > 0f || playerReference.playerStatistics.SleepSeconds > 0f || playerReference.playerStatistics.ParaSeconds > 0f)
            {
                classInfo.animator?.SetBool("stun", true);
            }
            else
            {
                classInfo.animator?.SetBool("stun", false);
            }
        }

        if (playerStats.StunSeconds > 0f)
        {
            playerStats.StunSeconds -= Time.deltaTime;
            if (IsClient && statesPrefab[0] == null)
            {
                statesPrefab[0] = Instantiate(Resources.Load("Prefabs/States/Stun") as GameObject, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            if (IsClient) Destroy(statesPrefab[0]);
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isStunned = false;
        }

        if (playerStats.ServerBlockSeconds > 0f)
        {
            playerStats.ServerBlockSeconds -= Time.deltaTime;
        }
        else
        {
            if (IsClient) Destroy(statesPrefab[0]);
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isServerBlock = false;
        }


        if (playerStats.FreezeSeconds > 0)
        {
            playerStats.FreezeSeconds -= Time.deltaTime;
            if (IsClient && statesPrefab[1] == null)
            {
                statesPrefab[1] = Instantiate(Resources.Load("Prefabs/States/Freeze") as GameObject, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isFrozen = false;
            if (IsClient) Destroy(statesPrefab[1]);
        }



        if (playerStats.SleepSeconds > 0)
        {
            playerStats.SleepSeconds -= Time.deltaTime;
            if (IsClient && statesPrefab[2] == null)
            {
                statesPrefab[2] = Instantiate(Resources.Load("Prefabs/States/Sleep") as GameObject, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isSleeping = false;
            if (IsClient) Destroy(statesPrefab[2]);
        }




        if (playerStats.ParaSeconds > 0)
        {
            playerStats.ParaSeconds -= Time.deltaTime;
            if (IsClient && statesPrefab[3] == null)
            {
                statesPrefab[3] = Instantiate(Resources.Load("Prefabs/States/Para") as GameObject, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            if (IsClient) Destroy(statesPrefab[3]);
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isParalyzed = false;
        }


        if (playerStats.StunAirSeconds > 0)
        {
            playerStats.StunAirSeconds -= Time.deltaTime;
            if (IsClient && statesPrefab[4] == null)
            {
                statesPrefab[4] = Instantiate(Resources.Load("Prefabs/States/StunAir") as GameObject, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            if (IsClient) Destroy(statesPrefab[4]);
            if (IsClient && !classInfo.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isStunnedAir = false;
        }

        if (playerStats.StunImmunitySeconds > 0f)
        {
            playerStats.StunImmunitySeconds -= Time.deltaTime;
        }

        bool stunned = playerStats.StunAirSeconds > 0f || playerStats.StunSeconds > 0f ||
                        playerStats.FreezeSeconds > 0f || playerStats.SleepSeconds > 0f ||
                        playerStats.ParaSeconds > 0f;

        if (stunned)
        {
            stunAccumulated += Time.deltaTime;
            if (stunAccumulated >= 5f)
            {
                playerStats.StunSeconds = 0f;
                playerStats.StunAirSeconds = 0f;
                playerStats.FreezeSeconds = 0f;
                playerStats.SleepSeconds = 0f;
                playerStats.ParaSeconds = 0f;
                playerStats.StunImmunitySeconds = 2.5f;
                stunAccumulated = 0f;
            }
        }
        else
        {
            stunAccumulated = 0f;
        }



        if (IsLocalPlayer || classInfo.isMonster)
        {
            if (!classInfo.isMonster)
            {
                RaycastHit hit;
                if (isPreparingAoe != -1 && !isFastCast)
                {
                    if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out hit, classInfo.spells[isPreparingAoe].spellRange, layerMask))
                    {
                        if (aoeZone == null)
                        {
                            // On v?rifie qu'un cercle d'AOE a bien ?t? d?fini pour le sort, car si non cela fait un warning inutile.
                            if (classInfo.spells[isPreparingAoe].aoeCircle != null)
                            {
                                aoeZone = Instantiate(classInfo.spells[isPreparingAoe].aoeCircle, new Vector3(hit.point.x, hit.point.y + 0.3f, hit.point.z), classInfo.spells[isPreparingAoe].aoeCircle.transform.rotation);
                                aoeZone.SetActive(true);
                            }
                        }
                        if (aoeZone != null)
                        {
                            aoeZone.transform.position = hit.point;
                        }
                        respectHitDistance = true;
                    }
                    // On ne doit pas vraiment enlever car si non ?a lui cancel le sort d?s qu'il regarde trop loin.
                    // D'un autre c?t? ?a lui emp?che de tricher.
                    else
                    {
                        if (aoeZone != null)
                        {
                            Destroy(aoeZone);
                        }
                        respectHitDistance = false;
                    }
                }
            }

            //////////////////////////////////////////////////////////
            ///                    GESTION DES CoolDown DES SORTS       ///
            ///                    
            // Si le sort 1 est spawn...
            if (RealSpells[0] != null)
            {
                // ... et le CoolDown est disponible:
                if (classInfo.isMonster)
                {
                    if (classInfo.warriorSpell1CD <= 0)
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[0] = null;
                    }
                }
                else
                {
                    if ((!playerReference.PlayerReincarnation.IsReincarnation && classInfo.warriorSpell1CD <= 0) || (playerReference.PlayerReincarnation.IsReincarnation && classInfo.spiritSpell1CD <= 0))
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[0] = null;
                    }
                }
            }

            // Si le sort 2 est spawn...
            if (RealSpells[1] != null)
            {
                // ... et le CoolDown est disponible:
                if (classInfo.isMonster)
                {
                    if (classInfo.warriorSpell2CD <= 0)
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[1] = null;
                    }
                }
                else
                {
                    if ((!playerReference.PlayerReincarnation.IsReincarnation && classInfo.warriorSpell2CD <= 0) || (playerReference.PlayerReincarnation.IsReincarnation && classInfo.spiritSpell2CD <= 0))
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[1] = null;
                    }
                }
            }

            // Si le sort 3 est spawn...
            if (RealSpells[2] != null)
            {
                // ... et le CoolDown est disponible:
                if (classInfo.isMonster)
                {
                    if (classInfo.warriorSpell3CD <= 0)
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[2] = null;
                    }
                }
                else
                {
                    if ((!playerReference.PlayerReincarnation.IsReincarnation && classInfo.warriorSpell3CD <= 0) || (playerReference.PlayerReincarnation.IsReincarnation && classInfo.spiritSpell3CD <= 0))
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[2] = null;
                    }
                }
            }

            // Si le sort 4 est spawn...
            if (RealSpells[3] != null)
            {
                // ... et le CoolDown est disponible:
                if (classInfo.isMonster)
                {
                    if (classInfo.warriorSpell4CD <= 0)
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[3] = null;
                    }
                }
                else
                {
                    if ((!playerReference.PlayerReincarnation.IsReincarnation && classInfo.warriorSpell4CD <= 0) || (playerReference.PlayerReincarnation.IsReincarnation && classInfo.spiritSpell4CD <= 0))
                    {
                        // On supprime le sort de la liste car on en aura plus besoin.
                        RealSpells[3] = null;
                    }
                }
            }


            // On r?duit le CoolDown
            if (classInfo.warriorSpell1CD > 0)
            {
                classInfo.warriorSpell1CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.warriorSpell1CD != 0 && !classInfo.isMonster)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.warriorSpell1CD = 0;
                if (playerReference.PlayerReincarnation == null || !playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[0] = false;
                }
            }

            if (classInfo.warriorSpell2CD > 0)
            {
                classInfo.warriorSpell2CD -= Time.deltaTime;

            }
            else
            {
                if (classInfo.warriorSpell2CD != 0 && !classInfo.isMonster)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.warriorSpell2CD = 0;
                if (playerReference.PlayerReincarnation == null || !playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[1] = false;
                }
            }

            if (classInfo.warriorSpell3CD > 0)
            {
                classInfo.warriorSpell3CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.warriorSpell3CD != 0 && !classInfo.isMonster)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.warriorSpell3CD = 0;
                if (playerReference.PlayerReincarnation == null || !playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[2] = false;
                }
            }

            if (classInfo.warriorSpell4CD > 0)
            {
                classInfo.warriorSpell4CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.warriorSpell4CD != 0 && !classInfo.isMonster)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.warriorSpell4CD = 0;
                if (playerReference.PlayerReincarnation == null || !playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[3] = false;
                }
            }


            // On r?duit le CoolDown
            if (classInfo.spiritSpell1CD > 0)
            {
                classInfo.spiritSpell1CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.spiritSpell1CD != 0)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.spiritSpell1CD = 0;
                if (playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[0] = false;
                }
            }

            if (classInfo.spiritSpell2CD > 0)
            {
                classInfo.spiritSpell2CD -= Time.deltaTime;

            }
            else
            {
                if (classInfo.spiritSpell2CD != 0)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.spiritSpell2CD = 0;
                if (playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[1] = false;
                }
            }

            if (classInfo.spiritSpell3CD > 0)
            {
                classInfo.spiritSpell3CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.spiritSpell3CD != 0)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.spiritSpell3CD = 0;
                if (playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[2] = false;
                }
            }

            if (classInfo.spiritSpell4CD > 0)
            {
                classInfo.spiritSpell4CD -= Time.deltaTime;
            }
            else
            {
                if (classInfo.spiritSpell4CD != 0)
                {
                    cooldownUI.instance.UpdateUiInformation();
                }
                classInfo.spiritSpell4CD = 0;
                if (playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation)
                {
                    classInfo.cancelSpells[3] = false;
                }
            }

            //////////////////////////////////////////////////////////
            //////////////////     GESTION DES SORTS     /////////////
            //////////////////////////////////////////////////////////
            ///                         GUERRIER                   ///
            //////////////////////////////////////////////////////////

        }

    }

    private void RotateToMouseDirection(Vector3 destination)
    {
        direction = destination - castProjectilePosition.position;
        // direction = (destination - castPositionNet.transform.position).normalized;
        rotation = Quaternion.LookRotation(direction);
        castProjectilePosition.rotation = rotation;
        directionY.Value = castProjectilePosition.rotation;
    }

    private bool ShouldUseCast(Spell spell, bool isPlayer)
    {
        return isPlayer && spell != null && spell.useCastTime && spell.castDuration > 0f && !classInfo.isMonster;
    }

    private void ApplyCooldownValue(int index, bool isSpiritForm, float value)
    {
        switch (index)
        {
            case 0:
                if (isSpiritForm)
                {
                    classInfo.spiritSpell1CD = value;
                }
                else
                {
                    classInfo.warriorSpell1CD = value;
                }
                break;
            case 1:
                if (isSpiritForm)
                {
                    classInfo.spiritSpell2CD = value;
                }
                else
                {
                    classInfo.warriorSpell2CD = value;
                }
                break;
            case 2:
                if (isSpiritForm)
                {
                    classInfo.spiritSpell3CD = value;
                }
                else
                {
                    classInfo.warriorSpell3CD = value;
                }
                break;
            case 3:
                if (isSpiritForm)
                {
                    classInfo.spiritSpell4CD = value;
                }
                else
                {
                    classInfo.warriorSpell4CD = value;
                }
                break;
        }
    }

    private void PerformSpellLaunch(Spell spell, int index, bool isPlayer)
    {
        if (spell.spellType != 1)
        {
            if (isPlayer)
            {
                LaunchSpellServerRpc(gameObject, index);
            }
            else
            {
                LaunchSpellGlobal(gameObject, index);
                LaunchSpellClientRpc(base.gameObject, index);
            }
        }
        else if ((isPreparingAoe != -1 && respectHitDistance) || isFastCast)
        {
            if (aoeZone == null)
            {
                RaycastHit hit;
                if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out hit, classInfo.spells[index].spellRange, layerMask))
                {
                    if (isPlayer)
                    {
                        LaunchSpellServerRpc(base.gameObject, index, hit.point);
                    }
                    else
                    {
                        LaunchSpellGlobal(gameObject, index, hit.point);
                        LaunchSpellClientRpc(base.gameObject, index, false, hit.point);
                    }
                    return;
                }
            }
            else
            {
                if (isPlayer)
                {
                    LaunchSpellServerRpc(base.gameObject, index, aoeZone.transform.position);
                }
                else
                {
                    Vector3 targetPosition = playerReference.follow.cible.transform.position + Vector3.up * multiplierUp;

                    LaunchSpellGlobal(gameObject, index, targetPosition);
                    LaunchSpellClientRpc(base.gameObject, index, false, targetPosition);
                }

                if (aoeZone != null)
                {
                    isPreparingAoe = -1;
                    Destroy(aoeZone);
                    aoeZone = null;
                }

                return;
            }
        }
        else
        {
            if (spell.spellType == 1 && isPreparingAoe == -1 && !isFastCast)
            {
                isPreparingAoe = index;
            }
            else
            {
                if (isPlayer)
                {
                    LaunchSpellServerRpc(base.gameObject, index);
                }
                else
                {
                    LaunchSpellGlobal(gameObject, index, aoeZonePosition: playerReference.follow.cible.transform.position);

                    LaunchSpellClientRpc(base.gameObject, index, aoeZonePosition: playerReference.follow.cible.transform.position);
                }
            }
        }
    }

    private void LogSpellBlocked(int index, string reason)
    {
        Debug.Log($"[Spell] Blocked slot {index}: {reason}");
    }

    private bool LogCastCancel(string reason)
    {
        lastCastCancelReason = reason;
        Debug.Log($"[CastCancel] {reason}");
        return true;
    }

    public void ExecuteSpell(int index, float cd, bool isPlayer = true)
    {
        if (classInfo == null || classInfo.spells == null || index < 0 || index >= classInfo.spells.Length)
        {
            LogSpellBlocked(index, "classInfo/spells not ready or index out of range");
            return;
        }

        Spell spell = classInfo.spells[index];

        if (spell == null)
        {
            LogSpellBlocked(index, "spell asset is null");
            return;
        }

        if (RealSpells[index] != null && RealSpells[index].isCancel)
        {
            CancelSpell(index, true);
            return;
        }

        if (currentCastingSpell != null && index != currentCastingIndex)
        {
            LogSpellBlocked(index, "another spell is currently casting");
            castCancelRequestedByOtherSpell = true;
            return;
        }
        if (currentCastingSpell != null && index == currentCastingIndex)
        {
            LogSpellBlocked(index, "same spell already casting");
            castCancelRequestedByOtherSpell = true;
            return;
        }

        if (playerStats.ServerBlockSeconds > 0f || playerStats.StunAirSeconds > 0f || playerStats.StunSeconds > 0f || playerStats.FreezeSeconds > 0f || playerStats.SleepSeconds > 0f || playerStats.ParaSeconds > 0f)
        {
            return;
        }

        if (playerStats.playerStatData.health <= 0 || playerReference.playerStatistics.isDead)
        {
            Debug.Log("blocked spell by isDead ");
            return;
        }

        if (isPlayer && playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting && currentCastingSpell == null)
        {
            if (RealSpells[index] != null)
            {
                CancelSpell(index, true);
            }
            Debug.Log("blocked spell by isCasting ");
            return;
        }

        if (playerReference.playerClasses.isBlockingOtherSpells && !playerReference.playerClasses.spells[index].overrideBlocking)
        {
            Debug.Log("blocked spell by isBlockingOtherSpells ");
            return;
        }

        if (playerReference.playerStatistics.isBehindWho)
        {
            Debug.Log("blocked spell by isBehindWho ");
            return;
        }

        if (cd != 0f)
        {
            LogSpellBlocked(index, $"cooldown active ({cd:0.00}s)");
            return;
        }

        if (playerReference.characterActor != null && playerReference.characterActor.IsGrounded && spell.blockCastingOnGrounded)
        {
            Debug.Log("Sort ? utiliser en l'air !");
            return;
        }

        bool useSpiritForm = playerReference.PlayerReincarnation != null && playerReference.PlayerReincarnation.IsReincarnation;
        bool shouldApplyCooldown = spell.spellType != 1 || (spell.spellType == 1 && isPreparingAoe != -1 && respectHitDistance) || isFastCast || !isPlayer;

        float? appliedCooldown = null;
        if (shouldApplyCooldown)
        {
            float baseCooldown = isPlayer ? spell.spellCooldown : (spell.spellMinCooldown / 2f);

            var zone = Run.instance.CPUcontroller.zone;
            bool isSurvivorMode = zone != null && zone.serverMode == GameMode.Survivor;
            bool isStreamerMode = zone != null && zone.serverMode == GameMode.Streamer;

            if (isPlayer)
            {
                if (isSurvivorMode || isStreamerMode)
                {
                    int stacks = 0;

                    if (stacks > 0)
                    {
                        float factor = Mathf.Max(0.1f, 1f - 0.20f * stacks);
                        baseCooldown = baseCooldown * factor;
                    }
                }
                else
                {
                    baseCooldown -= playerReference.playerStatistics.playerStatData.soul * 0.5f;
                }
            }

            baseCooldown = Mathf.Max(0f, baseCooldown);

            float finalCooldown = isSurvivorMode
                ? baseCooldown
                : Mathf.Max(spell.spellMinCooldown, baseCooldown);

            appliedCooldown = finalCooldown;
            ApplyCooldownValue(index, useSpiritForm, finalCooldown);
        }

        bool needCast = ShouldUseCast(spell, isPlayer);
        if (needCast && shouldApplyCooldown)
        {
            if (currentCastRoutine != null)
            {
                StopCoroutine(currentCastRoutine);
            }
            castCancelRequestedByOtherSpell = false;
            castCancelRequestedByTransformation = false;
            castCancelRequestedByAutoAttack = false;
            currentCastingSpell = spell;
            currentCastingIndex = index;
            currentCastRoutine = StartCoroutine(CastSpellRoutine(spell, index, isPlayer, appliedCooldown, useSpiritForm));
            return;
        }

        PerformSpellLaunch(spell, index, isPlayer);
    }

    private IEnumerator CastSpellRoutine(Spell spell, int index, bool isPlayer, float? appliedCooldown, bool usedSpiritForm)
    {
        Slider bar = GetOrCreateCastBar();
        float elapsed = 0f;
        bool startedCastAnimation = false;

        if (bar != null)
        {
            bar.maxValue = spell.castDuration;
            bar.value = 0f;
        }

        if (spell.castVfxPrefab != null)
        {
            currentCastVfx = Instantiate(spell.castVfxPrefab, transform.position, transform.rotation, transform);
        }

        // Ne pas lancer l'animation si déjà cancelable dès le départ
        if (ShouldCancelCast(spell))
        {
            RefundCooldown(appliedCooldown, usedSpiritForm, index);
            ClearCastState();
            yield break;
        }

        if (!string.IsNullOrEmpty(spell.castAnimationName))
        {
            classInfo.animator.Play(spell.castAnimationName);
            startedCastAnimation = true;
        }
        else if (spell.isAnimationPlay)
        {
            classInfo.animator.Play(spell.animationName);
            startedCastAnimation = true;
        }
        else if (spell.isAnimationParameter)
        {
            classInfo.animator.SetBool(spell.animationName, true);
            startedCastAnimation = true;
        }

        wasHitDuringCast = false;
        wasPushedDuringCast = false;

        while (elapsed < spell.castDuration)
        {
            if (ShouldCancelCast(spell))
            {
                StopCastAnimation(spell, startedCastAnimation);
                RefundCooldown(appliedCooldown, usedSpiritForm, index);
                ClearCastState();
                yield break;
            }

            elapsed += Time.deltaTime;
            if (bar != null)
            {
                bar.value = Mathf.Min(elapsed, spell.castDuration);
            }

            yield return null;
        }

        ClearCastState();
        PerformSpellLaunch(spell, index, isPlayer);
    }

    private bool ShouldCancelCast(Spell spell)
    {
        if (spell.cancelCastOnDash && playerReference.playerDash != null && playerReference.playerDash.isDashing)
        {
            return LogCastCancel("dash");
        }

        if (spell.cancelCastOnMove && playerReference.characterBrain != null)
        {
            var actions = playerReference.characterBrain.characterActions;
            float inputMag = actions.movement.value.magnitude;
            float threshold = spell.cancelCastMoveMinSpeed > 0f ? spell.cancelCastMoveMinSpeed : 0.01f;
            if (inputMag > threshold) return LogCastCancel($"move input {inputMag:0.00}>{threshold:0.00}");
        }

        if (spell.cancelCastOnTransformation && castCancelRequestedByTransformation)
        {
            return LogCastCancel("transformation");
        }

        if (spell.cancelCastOnAutoAttack && castCancelRequestedByAutoAttack)
        {
            return LogCastCancel("auto-attack during cast");
        }

        if (spell.cancelCastOnOtherSpell && castCancelRequestedByOtherSpell)
        {
            return LogCastCancel("other spell triggered cancel");
        }

        if (spell.cancelCastOnGround && playerReference.characterActor != null && playerReference.characterActor.IsGrounded)
        {
            return LogCastCancel("grounded");
        }

        if (spell.cancelCastOnAir && playerReference.characterActor != null && !playerReference.characterActor.IsGrounded)
        {
            return LogCastCancel("air");
        }

        if (spell.cancelCastOnJump && IsJumpPressed())
        {
            return LogCastCancel("jump input");
        }

        if (spell.cancelCastOnPush && wasPushedDuringCast)
        {
            return LogCastCancel("push");
        }

        if (spell.cancelCastOnHit && wasHitDuringCast)
        {
            return LogCastCancel("hit");
        }

        if (spell.cancelCastOnCrowdControl && (playerStats.ServerBlockSeconds > 0f || playerStats.StunAirSeconds > 0f || playerStats.StunSeconds > 0f || playerStats.FreezeSeconds > 0f || playerStats.SleepSeconds > 0f || playerStats.ParaSeconds > 0f))
        {
            return LogCastCancel("crowd control");
        }

        if (playerStats.playerStatData.health <= 0 || playerReference.playerStatistics.isDead)
        {
            return LogCastCancel("dead");
        }

        return false;
    }

    private Slider GetOrCreateCastBar()
    {
        if (castUiObj != null) return castUiObj;
        if (cooldownUI.instance == null) return null;

        GameObject prefab = Resources.Load("UI/bar/SpellBar") as GameObject;
        if (prefab == null) return null;

        var slider = Instantiate(prefab, cooldownUI.instance.transform).GetComponent<Slider>();
        slider.maxValue = 1f;
        slider.value = 0f;
        castUiObj = slider;
        return castUiObj;
    }

    private void ClearCastState()
    {
        if (castUiObj != null)
        {
            Destroy(castUiObj.gameObject);
            castUiObj = null;
        }
        if (currentCastVfx != null)
        {
            Destroy(currentCastVfx);
            currentCastVfx = null;
        }
        currentCastingSpell = null;
        currentCastingIndex = -1;
        currentCastRoutine = null;
        lastCastCancelReason = null;
        castCancelRequestedByOtherSpell = false;
        castCancelRequestedByTransformation = false;
        castCancelRequestedByAutoAttack = false;
        wasHitDuringCast = false;
        wasPushedDuringCast = false;
    }

    private void RefundCooldown(float? cooldownValue, bool usedSpiritForm, int index)
    {
        if (!cooldownValue.HasValue) return;
        ApplyCooldownValue(index, usedSpiritForm, 0f);
    }

    private bool IsJumpPressed()
    {
        try
        {
            var jumpAction = InputManager.inputActions?.Player.Jump;
            if (jumpAction == null) return false;

            // triggered couvre le frame de l'appui; sinon on teste la valeur en cours (utile pour maintien)
            if (jumpAction.triggered) return true;

            float val = 0f;
            try { val = jumpAction.ReadValue<float>(); } catch { }
            return val > 0.1f;
        }
        catch
        {
            return false;
        }
    }

    private void StopCastAnimation(Spell spell, bool startedCastAnimation)
    {
        if (classInfo?.animator == null) return;

        bool shouldForceIdle = lastCastCancelReason != "jump input" && lastCastCancelReason != "dash";

        try
        {
            if (spell.isAnimationParameter)
            {
                classInfo.animator.SetBool(spell.animationName, false);
            }

            if (spell.spellPriority && classInfo.animator.GetBool("spellPriority"))
            {
                classInfo.animator.SetBool("spellPriority", false);
            }

            if (shouldForceIdle && (startedCastAnimation || spell.isAnimationParameter || spell.isAnimationPlay)) // pas de retour idle si le cancel vient d'un saut ou d'un dash
            {
                // Revenir sur une base neutre
                classInfo.animator.CrossFadeInFixedTime("idle", 0.05f);
            }

            if (!classInfo.isMonster && playerReference?.characterBrain != null)
            {
                var handler = playerReference.characterBrain.inputHandlerSettings.InputHandler;
                handler.isCasting = false;
                handler.blockMove = false;
                handler.ForceUpdateMovement();
            }
        }
        catch { }
    }

    private void OnLocalHealthChangedDuringCast(float previousHealth, float currentHealth)
    {
        if (currentCastRoutine == null) return;
        if (currentHealth < previousHealth - Mathf.Epsilon)
        {
            wasHitDuringCast = true;
        }
    }

    private void MarkPushedDuringCast()
    {
        if (currentCastRoutine == null) return;
        wasPushedDuringCast = true;
    }

    public void NotifyAutoAttackDuringCast()
    {
        castCancelRequestedByAutoAttack = true;
    }

    public void HandleTransformationEvent()
    {
        castCancelRequestedByTransformation = true;
        HandleTransformationCleanup();
    }

    private void HandleTransformationCleanup()
    {
        bool releasedEffects = TryCancelActiveSpellsOnTransform(classInfo.humanRealSpells);
        releasedEffects |= TryCancelActiveSpellsOnTransform(classInfo.reincarnationRealSpells);
        releasedEffects |= CleanupCasterControlTimelinesOnTransform();
        releasedEffects |= StopActivePushesOnTransform();

        if ((releasedEffects || currentCastingSpell != null) && !classInfo.isMonster && playerReference?.characterBrain != null)
        {
            var handler = playerReference.characterBrain.inputHandlerSettings.InputHandler;
            handler.blockMove = false;
            handler.isCasting = false;
            handler.ForceUpdateMovement();

            if (playerReference.playerMovement != null)
            {
                playerReference.playerMovement.movementSpeedFactors.Clear();
                playerReference.playerMovement.jumpBlockers.Clear();
                playerReference.playerMovement.dashBlockers.Clear();
            }
        }
        else if (releasedEffects && classInfo.isMonster && playerReference.follow != null)
        {
            playerReference.follow.isInBlockMove = false;
            playerReference.follow.blockCast = false;
        }
    }

    private bool TryCancelActiveSpellsOnTransform(Spell[] realSpellArray)
    {
        bool modified = false;
        if (realSpellArray == null) return modified;

        foreach (var activeSpell in realSpellArray)
        {
            if (activeSpell == null) continue;

            bool destroyAll = activeSpell.destroyEffectOnTransformation;
            bool cancelOnly = activeSpell.cancelEffectsOnTransformation;
            if (!destroyAll && !cancelOnly) continue;

            int idx = activeSpell.CasterSpellIndex;
            if (idx >= 0 && idx < casterControlCoroutines.Length && casterControlCoroutines[idx] != null)
            {
                try { StopCoroutine(casterControlCoroutines[idx]); } catch { }
                casterControlCoroutines[idx] = null;
            }
            // Always clear timeline effects applied on the caster (speed factors, jump/dash blocks)
            activeSpell.CancelCasterTimelineImmediate(playerReference);

            if (destroyAll)
            {
                // Hard-destroy: kill the instance and clear both real-spell slots
                if (idx >= 0)
                {
                    if (idx < classInfo.humanRealSpells.Length) classInfo.humanRealSpells[idx] = null;
                    if (idx < classInfo.reincarnationRealSpells.Length) classInfo.reincarnationRealSpells[idx] = null;
                }
                Destroy(activeSpell.gameObject);
                if (activeSpell.blockOtherSpell) classInfo.isBlockingOtherSpells = false;
                modified = true;
                continue;
            }

            if (cancelOnly)
            {
                activeSpell.StopCasterTimelineAndClearEffects();
                if (idx >= 0 && idx < classInfo.cancelSpells.Length)
                {
                    StartCoroutine(TemporarilyFlagCancel(idx));
                }
                if (activeSpell.blockOtherSpell) classInfo.isBlockingOtherSpells = false;
                modified = true;
            }
        }

        return modified;
    }

    private IEnumerator TemporarilyFlagCancel(int index)
    {
        classInfo.cancelSpells[index] = true;
        yield return null;
        classInfo.cancelSpells[index] = false;
    }

    private bool CleanupCasterControlTimelinesOnTransform()
    {
        bool modified = false;
        for (int i = 0; i < casterControlCoroutines.Length; i++)
        {
            var spellAsset = classInfo.spells.Length > i ? classInfo.spells[i] : null;
            if (spellAsset == null) continue;

            bool shouldClear = spellAsset.cancelEffectsOnTransformation || spellAsset.destroyEffectOnTransformation;
            if (!shouldClear) continue;

            if (casterControlCoroutines[i] != null)
            {
                try { StopCoroutine(casterControlCoroutines[i]); } catch { }
                casterControlCoroutines[i] = null;
            }

            spellAsset.CancelCasterTimelineImmediate(playerReference);
            if (spellAsset.blockOtherSpell) classInfo.isBlockingOtherSpells = false;
            modified = true;
        }

        if (playerReference != null && playerReference.playerMovement != null)
        {
            playerReference.playerMovement.movementSpeedFactors.Clear();
            playerReference.playerMovement.jumpBlockers.Clear();
            playerReference.playerMovement.dashBlockers.Clear();
        }
        return modified;
    }

    private bool StopActivePushesOnTransform()
    {
        if (activePushCoroutines.Count == 0) return false;
        foreach (var c in activePushCoroutines)
        {
            if (c != null)
            {
                try { StopCoroutine(c); } catch { }
            }
        }
        activePushCoroutines.Clear();

        if (playerReference != null && playerReference.characterActor != null)
        {
            playerReference.characterActor.Velocity = Vector3.zero;
            playerReference.characterActor.ForceGrounded();
        }

        return true;
    }

    public void LaunchAutoAttack(Spell spell, int index)
    {
        Quaternion adjustedRotation = castNormalPosition.transform.rotation * Quaternion.Euler(-2, 0, 0);
        Quaternion rotationCommon = spell.forceIdentityRotation
            ? Quaternion.identity
            : (spell.spellType == 0
                ? (spell.isProjectilePosition
                    ? (playerReference.playerClasses.isMonster ? adjustedRotation : directionY.Value)
                    : Quaternion.Euler(0, directionY.Value.eulerAngles.y, 0))
                : Quaternion.Euler(0, directionY.Value.eulerAngles.y, 0));
        Vector3 positionCommon = spell.spellType == 1 ? aoeZone.transform.position : (spell.isProjectilePosition ? castProjectilePosition.transform.position : castNormalPosition.transform.position);
        LaunchSpellServerRpc(gameObject, index, default, true);
    }


    /// <summary>
    /// positionAOE is used only for AOE spell, else, calculated ServerSide.
    /// </summary>
    /// <param name="casterObject"></param>
    /// <param name="index"></param>
    /// <param name="positionAOE"></param>
    /// <param name="isAuto"></param>
    /// <param name="serverParam"></param>
    [ServerRpc]
    public void LaunchSpellServerRpc(NetworkObjectReference casterObject, int index, Vector3 positionAOE = default, bool isAuto = false, ServerRpcParams serverParam = default)
    {

        if (casterObject.TryGet(out NetworkObject casterNet))
        {
            GameObject caster = casterNet.gameObject;
            LaunchSpellGlobal(caster, index, positionAOE, isAuto);
            LaunchSpellClientRpc(caster, index, isAuto, positionAOE);
        }
    }

    [ClientRpc]
    public void LaunchSpellClientRpc(NetworkObjectReference casterObject, int index, bool isAuto = false, Vector3 aoeZonePosition = default, ClientRpcParams clientParam = default)
    {
        if (casterObject.TryGet(out NetworkObject casterNet))
        {
            GameObject caster = casterNet.gameObject;
            LaunchSpellGlobal(caster, index, default, isAuto);
        }
    }

    public void LaunchSpellGlobal(GameObject casterObject, int index, Vector3 aoeZonePosition = default, bool isAuto = false)
    {
        Spell spell = isAuto ? playerReference.playerClasses.autoAttacks[index] : playerReference.playerClasses.spells[index];

        // If the previous spell was cancelled (and its object already destroyed),
        // make sure the cancel flag does not block new casts
        if (RealSpells[index] == null && classInfo.cancelSpells[index])
        {
            classInfo.cancelSpells[index] = false;
        }
        // Si on est ? un etat impossible
        if (playerStats.ServerBlockSeconds > 0f || playerStats.StunAirSeconds > 0f || playerStats.StunSeconds > 0f || playerStats.FreezeSeconds > 0f || playerStats.SleepSeconds > 0f || playerStats.ParaSeconds > 0f)
        {
            return;
        }
        // Si le joueur est mort.
        if (playerStats.playerStatData.health <= 0 || playerReference.playerStatistics.isDead)
        {
            return;
        }

        if (!playerReference.playerClasses.isMonster)
        {
            if (playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting)
            {
                if (RealSpells[index] != null)
                {
                    CancelSpell(index, true);
                }
                return;
            }
        }

        if (playerReference.playerClasses.isBlockingOtherSpells)
        {
            // Autoriser les sorts qui peuvent ignorer le blocage (overrideBlocking)
            if (!playerReference.playerClasses.spells[index].overrideBlocking)
            {
                return;
            }
        }
        if (spell.blockOtherSpell)
        {
            playerReference.playerClasses.isBlockingOtherSpells = true;
        }

        if (spell.CastPrefab != null)
        {
            GameObject gameObjectBuff = ((
                spell.castPosition == 0) ? Instantiate(spell.CastPrefab, castNormalPosition.position, castNormalPosition.rotation) :
                ((spell.castPosition != 1) ? Instantiate(spell.CastPrefab, castProjectilePosition.position, castProjectilePosition.rotation) :
                Instantiate(spell.CastPrefab, castNormalPosition.position, castNormalPosition.rotation)));
            gameObjectBuff.GetComponent<Buff>()?.SetCaster(gameObject);
            if (gameObject.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                gameObjectBuff.GetComponent<CinemachineImpulseSource>()?.GenerateImpulse();
            }
        }
        if (spell.spellPriority)
        {
            if (classInfo.isMonster)
            {
                classInfo.animator.SetBool("spellPriority", value: true);
            }
            else
            {
                classInfo.animator.SetBool("spellPriority", value: true);
            }
        }
        if (spell.blockMoveSeconds > 0f && !playerReference.playerClasses.isMonster)
        {
            StartCoroutine(BlockMoveSeconds(spell));
        }

        // Apply caster control timeline if configured
        if (spell.casterControlTimeline != null && spell.casterControlTimeline.Count > 0)
        {
            // Stop any previous timeline for this slot
            if (casterControlCoroutines[index] != null)
            {
                try { StopCoroutine(casterControlCoroutines[index]); } catch { }
                casterControlCoroutines[index] = null;
            }
            // Ensure previous effects from that slot are fully cleared
            try { spell.CancelCasterTimelineImmediate(playerReference); } catch { }
            casterControlCoroutines[index] = StartCoroutine(spell.ApplyCasterControlTimeline(playerReference));
        }

        if (spell.blockCastingSpellsSeconds > 0f)
        {
            StartCoroutine(BlockCastingSeconds(spell));
        }

        if (spell.isAnimationParameter)
        {
            Debug.Log("On lance l'animation : " + spell.animationName);
            if (classInfo.isMonster)
            {
                classInfo.animator.SetBool(spell.animationName, value: true);
            }
            else
            {
                classInfo.animator.SetBool(spell.animationName, value: true);
            }
        }
        if (spell.isAnimationPlay)
        {
            classInfo.animator.Play(spell.animationName);
        }
        switch (spell.spellType)
        {
            case 0:
                StartCoroutine(LaunchSpell(spell, casterObject, index, aoeZonePosition, isAuto));
                break;
            case 1:
                isPreparingAoe = -1;
                StartCoroutine(LaunchSpell(spell, casterObject, index, aoeZonePosition, isAuto));
                break;
        }
        if (spell.pushCasterList != null && spell.pushCasterList.Count > 0)
        {
            if (casterObject.layer == 7)
            {
#if UNITY_SERVER
                PushMonster(casterObject, spell, index);
#endif
            }
            else
            {
                PushPlayer(casterObject, spell, index);
            }
        }
        if (spell.isAnimationParameter)
        {
            StartCoroutine(RemoveAnimation(spell, index));
        }
        if (spell.isCancel)
        {
            StartCoroutine(CancelSpellCoroutine(spell, index));
        }
        if (!spell.isAnimationParameter && spell.spellPriority)
        {
            StartCoroutine(CancelSpellPriority(spell));
        }
    }

    public IEnumerator BlockMoveSeconds(Spell spell)
    {
        if (!classInfo.isMonster)
        {
            playerReference.characterBrain.inputHandlerSettings.InputHandler.blockMove = true;
            playerReference.characterBrain.characterActions.movement.value = Vector2.zero;
        }
        else
        {
            if (playerReference.follow != null)
            {
                playerReference.follow.isInBlockMove = true;
            }
        }

        yield return new WaitForSeconds(spell.blockMoveSeconds);

        if (!classInfo.isMonster)
        {
            playerReference.characterBrain.inputHandlerSettings.InputHandler.blockMove = false;
            playerReference.characterBrain.inputHandlerSettings.InputHandler.ForceUpdateMovement();
        }
        else
        {
            if (playerReference.follow != null)
            {
                playerReference.follow.isInBlockMove = false;
            }
        }
    }
    public IEnumerator BlockCastingSeconds(Spell spell)
    {
        if (!classInfo.isMonster)
        {
            playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting = true;
            playerReference.characterBrain.characterActions.movement.value = Vector2.zero;
        }
        else
        {
            playerReference.follow.blockCast = true;
        }
        yield return new WaitForSeconds(spell.blockCastingSpellsSeconds);
        if (!classInfo.isMonster)
        {
            playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting = false;
            playerReference.characterBrain.inputHandlerSettings.InputHandler.ForceUpdateMovement();
        }
        else
        {
            playerReference.follow.blockCast = false;
        }
        //OLD: mouvementJoueur.SetBlockMove(false);
    }

    public IEnumerator CancelSpellPriority(Spell spell)
    {
        yield return new WaitForSeconds(spell.spellPrioritySeconds);
        if (classInfo.isMonster)
        {
            if (classInfo.animator.GetBool("spellPriority")) classInfo.animator.SetBool("spellPriority", false);
            // mouvementJoueur.ApplyAnimationMonsterClientRpc(classInfo.gameObject, "spellPriority", false);
        }
        else
        {
            //mouvementJoueur.ApplyAnimationClientRpc("spellPriority", false);
            if (classInfo.animator.GetBool("spellPriority"))
                classInfo.animator.SetBool("spellPriority", false);
        }
    }

    /*    [ServerRpc]
        void AskShootingServerRpc(bool value)
        {
            shooting.Value = value;
        }*/
    void PushPlayer(GameObject caster, Spell spell, int index)
    {
        if (IsLocalPlayer)
        {
            PlayerClasses casterClass = caster.GetComponent<PlayerClasses>();

            if (spell.pushCasterList != null && spell.pushCasterList.Count > 0)
            {
                foreach (PushType pushType in spell.pushCasterList)
                {
                    // Empêcher la création d'une coroutine si le sort a été annulé
                    if (casterClass.cancelSpells.Length > index && casterClass.cancelSpells[index])
                        break;

                    // Vector3 direction, float startDelay, float endDelay, float pushPower, int index, PlayerClasses casterClass
                    var c = StartCoroutine(PushPlayer(pushType, index, casterClass, spell));
                    activePushCoroutines.Add(c);
                }
            }
        }
    }

    void PushMonster(GameObject caster, Spell spell, int index)
    {
        if (spell.pushCasterList == null || spell.pushCasterList.Count == 0) return;

        PlayerReference casterRef = caster.GetComponent<PlayerReference>();

        foreach (PushType pushType in spell.pushCasterList)
        {
            float remainingPushDuration = pushType.pushEndDelay - pushType.pushStartDelay;
            if (remainingPushDuration > 0f)
            {
                casterRef.StartCoroutine(DelayedTriggerMonsterPush(casterRef, pushType, remainingPushDuration));
            }
        }
    }

    private IEnumerator DelayedTriggerMonsterPush(PlayerReference casterRef, PushType pushType, float duration)
    {
        if (pushType.pushStartDelay > 0f)
            yield return new WaitForSeconds(pushType.pushStartDelay);

        Vector3 pushDirection = casterRef.transform.rotation * pushType.pushPower;
        // Appelle la coroutine `Push` déjà existante dans Follow
        casterRef.follow.TriggerPush(pushDirection, duration);
    }
    IEnumerator PushPlayer(PushType pushType, int index, PlayerClasses casterClass, Spell spell)
    {
        bool wasSpellReal = false;
        bool smearActivated = false;
        SmearEffectHelper smearHelper = playerReference != null ? playerReference.GetComponent<SmearEffectHelper>() : null;

        yield return new WaitForSeconds(pushType.pushStartDelay);

        if (smearHelper != null)
        {
            smearHelper.EnableSmear();
            smearActivated = true;
        }

        float timer = 0f;
        float pushBonus = 0;

        if (pushType.pushPower.y != 0 && playerReference.characterActor.IsGrounded)
        {
            playerReference.characterActor.ForceNotGrounded();
        }

        MarkPushedDuringCast();

        try
        {
            while (timer < pushType.pushEndDelay)
            {
                if (casterClass.cancelSpells[index]) break;

                if (RealSpells[index] != null)
                {
                    wasSpellReal = true;
                    if (RealSpells[index].animationName != spell.animationName)
                        break;
                }
                else if (wasSpellReal)
                {
                    Debug.Log("Le sort est cancel pour raison: wasSpellReal");
                    break;
                }

                // --- Recalcul de la direction de poussee a chaque frame ---
                Vector3 worldPush = casterClass.transform.forward * pushType.pushPower.z +
                                    casterClass.transform.right * pushType.pushPower.x +
                                    Vector3.up * pushType.pushPower.y;

                playerReference.characterActor.Velocity = worldPush;

                if (playerReference.characterActor.IsGrounded && pushType.stopOnGrounded)
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Reforce le grounded si necessaire
            if (pushType.pushPower.y == 0 || pushType.stopOnGrounded)
            {
                playerReference.characterActor.ForceGrounded();
            }
        }
        finally
        {
            if (smearActivated && smearHelper != null)
            {
                smearHelper.DisableSmear();
            }
        }

        if (casterClass.follow != null)
        {
            casterClass.follow.GetAgent().enabled = true;
        }

        activePushCoroutines.RemoveAll(c => c == null);
    }
    IEnumerator CheckGroundMonster(PlayerMovement move, Follow agent, Rigidbody rigidbody)
    {
        while (true)
        {
            //controller.transform.position = new Vector3(controller.transform.position.x, controller.transform.position.y - 0.3f, controller.transform.position.z);
            if (move.playerReference.characterActor.IsGrounded)
            {
                Debug.Log("Il touche le sol, on le descend, on r?active l'agent !");
                agent.enabled = true;
                agent.GetAgent().enabled = true;
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator CancelSpellCoroutine(Spell spell, int index)
    {
        for (float timer = spell.durationWhileCancelAvailable; timer >= 0; timer -= Time.deltaTime)
        {
            if (playerReference.playerStatistics.StunAirSeconds > 0f
                || playerReference.playerStatistics.StunSeconds > 0f
                || playerReference.playerStatistics.FreezeSeconds > 0f
                || playerReference.playerStatistics.SleepSeconds > 0f
                || playerReference.playerStatistics.ServerBlockSeconds > 0f
                || playerReference.playerStatistics.ParaSeconds > 0f)
            {
                classInfo.cancelSpells[index] = true;
            }

            // ?? SORTIE RAPIDE : si déjà annulé, pas besoin d'attendre
            if (classInfo.cancelSpells[index])
            {
                Debug.Log("Sort annulé immédiatement : " + index);
                Destroy(RealSpells[index]?.gameObject);
                StartCoroutine(CancelSpellAfterStopPush(classInfo, index));
                yield break;
            }

            yield return null;
        }

        // ?? Assurer que le statut est bien réinitialisé
        Debug.Log("Fin du timer, reset CancelSpell : " + index);
        CancelSpell(index, false);
    }

    IEnumerator CancelSpellAfterStopPush(PlayerClasses classInfo, int index)
    {
        yield return new WaitForSeconds(1.0f);
        classInfo.cancelSpells[index] = false;
        CancelSpell(index, false);
    }

    void ResetCancelStates()
    {
        // Nettoyer la zone AOE en cours si besoin
        if (aoeZone != null)
        {
            Destroy(aoeZone);
            aoeZone = null;
        }

        isPreparingAoe = -1;
        playerReference.playerStatistics.isInvincible = false;

        if (!classInfo.isMonster)
        {
            var handler = playerReference.characterBrain.inputHandlerSettings.InputHandler;
            handler.blockMove = false;
            handler.isCasting = false;
        }
        else if (playerReference.follow != null)
        {
            playerReference.follow.blockCast = false;
            playerReference.follow.isInBlockMove = false;
        }
    }


    public void CancelSpell(int index, bool status)
    {
        if (status && RealSpells[index] != null)
        {
            // Empêcher l'annulation dans la première seconde suivant le lancement
            if (Time.time - RealSpells[index].timeStarted < 1f)
            {
                Debug.Log("Trop tôt pour annuler le sort " + index);
                return;
            }
            // Détruire immédiatement l'instance du sort côté local
            // Stop caster-control timeline started here and clear effects before destroying
            var spellAsset = playerReference.playerClasses.spells[index];
            if (casterControlCoroutines[index] != null)
            {
                try { StopCoroutine(casterControlCoroutines[index]); } catch { }
                casterControlCoroutines[index] = null;
            }
            try { spellAsset.CancelCasterTimelineImmediate(playerReference); } catch { }
            Destroy(RealSpells[index].gameObject);
            RealSpells[index] = null;
        }
        classInfo.cancelSpells[index] = status;

        // Si le joueur annule un sort qui bloque les autres, lever le blocage
        if (status && RealSpells[index] != null && RealSpells[index].blockOtherSpell)
        {
            classInfo.isBlockingOtherSpells = false;
            Debug.Log("Blocage des sorts levé car le sort a été annulé.");
        }

        if (status)
        {
            ResetCancelStates();
        }

        CancelSpellServerRpc(index, status);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CancelSpellServerRpc(int index, bool status, ServerRpcParams serverParam = default)
    {
        if (!classInfo.isMonster)
        {
            if (status && RealSpells[index] != null)
            {
                if (Time.time - RealSpells[index].timeStarted < 1f)
                {
                    return;
                }
                // Détruire l'instance serveur du sort
                Destroy(RealSpells[index].gameObject);
                RealSpells[index] = null;
            }
            Debug.Log("Cancel spell " + index + " : " + status + "");
            classInfo.cancelSpells[index] = status;

            if (status && RealSpells[index] != null && RealSpells[index].blockOtherSpell)
            {
                classInfo.isBlockingOtherSpells = false;
                Debug.Log("Blocage des sorts levé suite à l'annulation.");
            }

            if (status)
            {
                ResetCancelStates();
            }

            if (playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting) playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting = false;
            CancelSpellClientRpc(index, status, serverParam.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    private void CancelSpellClientRpc(int index, bool status, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            return;
        }
        classInfo.cancelSpells[index] = status;

        if (status && RealSpells[index] != null)
        {
            // Détruire l'objet du sort sur les autres clients
            Destroy(RealSpells[index].gameObject);
            RealSpells[index] = null;
        }

        if (status && RealSpells[index] != null && RealSpells[index].blockOtherSpell)
        {
            classInfo.isBlockingOtherSpells = false;
            Debug.Log("Blocage des sorts levé suite à l'annulation.");
        }
        if (status)
        {
            ResetCancelStates();
        }
        Debug.Log("Cancel spell " + index + " : " + status + "");
    }


    /*    [ServerRpc]
        void CancelSpellServerRpc(NetworkObjectReference casterRef, string spellName, int index, bool status)
        {
            if (casterRef.TryGet(out NetworkObject casterNet))
            {
                casterNet.GetComponent<PlayerClasses>().cancelSpells[index] = status;
                CancelSpellClientRpc(casterRef, index, status);
            }
        }
        [ClientRpc]
        void CancelSpellClientRpc(NetworkObjectReference casterRef, int index, bool status)
        {
            if (IsClient)
            {
                if (casterRef.TryGet(out NetworkObject casterNet))
                {
                    casterNet.GetComponent<PlayerClasses>().cancelSpells[index] = status;
                }
            }
        }
    */


    IEnumerator RemoveAnimation(Spell spell, int index)
    {
        for (float timer = spell.durationBeforeRemoveParameter; timer >= 0; timer -= Time.deltaTime)
        {

            if (playerReference.playerStatistics.ServerBlockSeconds > 0f || playerReference.playerStatistics.StunAirSeconds > 0f || playerReference.playerStatistics.StunSeconds > 0f || playerReference.playerStatistics.FreezeSeconds > 0f || playerReference.playerStatistics.SleepSeconds > 0f || playerReference.playerStatistics.ParaSeconds > 0f)
            {
                classInfo.animator.SetBool(spell.animationName, false);
            }

            if (classInfo.cancelSpells[index])
            {
                classInfo.animator.SetBool(spell.animationName, false);
                //mouvementJoueur.ApplyAnimationClientRpc(spell.animationName, false);
                if (spell.spellPriority)
                {
                    if (classInfo.isMonster)
                    {
                        classInfo.animator.SetBool("spellPriority", false);
                        // mouvementJoueur.ApplyAnimationMonsterClientRpc(classInfo.gameObject, "spellPriority", false);
                    }
                    else
                    {
                        classInfo.animator.SetBool("spellPriority", false);
                        //mouvementJoueur.ApplyAnimationClientRpc("spellPriority", false);
                    }
                }
                if (!playerReference.playerClasses.isMonster) playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting = false;
                yield break;
            }
            yield return null;
        }
        if (classInfo.isMonster)
        {
            classInfo.animator.SetBool(spell.animationName, false);
            // mouvementJoueur.ApplyAnimationMonsterClientRpc(classInfo.gameObject, spell.animationName, false);
        }
        else
        {
            classInfo.animator.SetBool(spell.animationName, false);
            // mouvementJoueur.ApplyAnimationClientRpc(spell.animationName, false);
        }
        if (spell.spellPriority)
        {
            if (classInfo.isMonster)
            {
                classInfo.animator.SetBool("spellPriority", false);
                // mouvementJoueur.ApplyAnimationMonsterClientRpc(classInfo.gameObject, "spellPriority", false);
            }
            else
            {
                classInfo.animator.SetBool("spellPriority", false);
                //mouvementJoueur.ApplyAnimationClientRpc("spellPriority", false);
            }
        }
        if (spell.blockMoveSeconds > 0 && spell.isAnimationParameter && !playerReference.playerClasses.isMonster)
        {
            playerReference.characterBrain.inputHandlerSettings.InputHandler.isCasting = false;
        }
        if (spell.blockOtherSpell)
        {
            playerReference.playerClasses.isBlockingOtherSpells = false;
        }

    }

    IEnumerator LaunchSpell(Spell spell, GameObject caster, int index, Vector3 aoePosition = default, Boolean isAuto = false)
    {
        for (float timer = spell.durationBeforeEffect; timer >= 0; timer -= Time.deltaTime)
        {
            if (!spell.cannotBeCancelled &&
                (playerReference.playerStatistics.ServerBlockSeconds > 0f ||
                playerReference.playerStatistics.StunAirSeconds > 0f ||
                playerReference.playerStatistics.StunSeconds > 0f ||
                playerReference.playerStatistics.FreezeSeconds > 0f ||
                playerReference.playerStatistics.SleepSeconds > 0f ||
                playerReference.playerStatistics.ParaSeconds > 0f))
            {
                Debug.Log("STUNNED -> Sort annulé");
                yield break;
            }

            if (!spell.cannotBeCancelled && classInfo.cancelSpells[index])
            {
                yield break;
            }
            if (classInfo.cancelSpells[index])
            {
                //Debug.Log("Le sort a ?t? cancel, je ne vais pas lancer l'effet qui ?tait pr?vu.");
                yield break;
            }
            yield return null;
        }

        if (spell.invincibleTime > 0)
        {
            StartCoroutine(LaunchInvincible(spell, index));
        }

#if UNITY_SERVER
        Quaternion adjustedRotation = castNormalPosition.transform.rotation * Quaternion.Euler(-2, 0, 0);
        Quaternion rotationCommon = spell.forceIdentityRotation
            ? Quaternion.identity
            : (spell.spellType == 0
                ? (spell.isProjectilePosition
                    ? (playerReference.playerClasses.isMonster ? adjustedRotation : directionY.Value)
                    : Quaternion.Euler(0, directionY.Value.eulerAngles.y, 0))
                : Quaternion.Euler(0, directionY.Value.eulerAngles.y, 0));
        Vector3 positionCommon = spell.spellType == 1 ? (aoeZone != null ? aoeZone.transform.position : castNormalPosition.transform.position) : (spell.isProjectilePosition ? castProjectilePosition.transform.position : castNormalPosition.transform.position);

        switch (spell.spellType)
        {
            case 0:
                if (spell.isProjectilePosition)
                {
                    CreateSpellInstance(spell, positionCommon, rotationCommon, caster, index, isAuto);
                    CreateSpellInstanceClientRpc(isAuto, index, positionCommon, rotationCommon, caster);
                }
                else
                {
                    CreateSpellInstance(spell, positionCommon, rotationCommon, caster, index, isAuto);
                    CreateSpellInstanceClientRpc(isAuto, index, positionCommon, rotationCommon, caster);
                }
                break;
            case 1:
                CreateSpellInstance(spell, aoePosition, rotationCommon, caster, index, isAuto);
                CreateSpellInstanceClientRpc(isAuto, index, aoePosition, rotationCommon, caster);
                break;
        }
#endif
    }

    IEnumerator LaunchInvincible(Spell spell, int index)
    {
        for (float timer = spell.durationBeforeEffect; timer >= 0; timer -= Time.deltaTime)
        {
            if (classInfo.cancelSpells[index]) // Si annulé
            {
                playerReference.playerStatistics.isInvincible = false;
                Debug.Log("Invincibilité annulée (stun/sleep/cancel).");
                yield break;
            }

            playerReference.playerStatistics.isInvincible = true;
            yield return null;
        }

        yield return new WaitForSeconds(spell.invincibleTime);

        // S'assurer que l'invincibilité se désactive bien après le temps défini
        playerReference.playerStatistics.isInvincible = false;
    }

    void CreateSpellInstance(Spell spell, Vector3 position, Quaternion rotation, GameObject caster, int index, Boolean isAuto = false)
    {
        GameObject go;
        if (!spell.launchEffectToNewPosition)
            go = Instantiate(spell.gameObject, position, rotation);
        else if (!spell.isProjectilePosition)
            go = Instantiate(spell.gameObject, position, rotation);
        else
            go = Instantiate(spell.gameObject, position, rotation);
        go.GetComponent<Spell>().SetCaster(caster, index, isAuto);
        // AntiMagicSpell ne compte pas !
        if (IsLocalPlayer && go.layer != 20)
        {
            go.layer = 11;
        }
        if (!isAuto)
        {
            RealSpells[index] = go.GetComponent<Spell>();
        }
    }


    [ClientRpc]
    void CreateSpellInstanceClientRpc(bool isAuto, int index, Vector3 position, Quaternion rotation, NetworkObjectReference caster)
    {
        if (caster.TryGet(out NetworkObject casterNet))
        {
            Spell spell = isAuto ? playerReference.playerClasses.autoAttacks[index] : playerReference.playerClasses.spells[index];
            GameObject go;
            if (!spell.launchEffectToNewPosition)
                go = Instantiate(spell.gameObject, position, rotation);
            else if (!spell.isProjectilePosition)
                go = Instantiate(spell.gameObject, position, rotation);
            else
                go = Instantiate(spell.gameObject, position, rotation);
            go.GetComponent<Spell>().SetCaster(caster, index, isAuto);
            // AntiMagicSpell ne compte pas !
            if (IsLocalPlayer && go.layer != 20)
            {
                go.layer = 11;
            }
            if (!isAuto)
            {
                RealSpells[index] = go.GetComponent<Spell>();
            }
        }
    }

    public void PushTarget(PlayerReference otherRef, PushType pushInfo)
    {
        if (pushInfo.pushPower.y != 0)
        {
            if (otherRef.characterActor.IsGrounded)
                otherRef.characterActor.ForceNotGrounded(2);
        }

        // Si c'est un client, ce n'est pas au client de pousser le monstre.
        if (otherRef.gameObject.layer == 7 && IsClient)
            return;

        if (otherRef.gameObject.layer == 7)
        {
            otherRef.agent.enabled = false;
            StartCoroutine(CheckGroundMonster(otherRef.playerMovement, otherRef.follow, otherRef.rigidBody));
        }

        if (playerReference.rigidBody != null)
        {
            Vector3 pushVelocity = otherRef.characterActor.Rotation * pushInfo.pushPower;
            otherRef.characterActor.Velocity = pushVelocity;

            if (otherRef == playerReference)
            {
                MarkPushedDuringCast();
            }
        }
    }








    [ClientRpc]
    public void MoveTowardsTargetClientRpc(NetworkObjectReference netObject, Vector3 target, float power)
    {
        if (netObject.TryGet(out NetworkObject objectNet))
        {
            GameObject player = objectNet.gameObject;
            var playerRef = player.GetComponent<PlayerReference>();

            // Ajoute un mouvement bas? sur cette direction et le pouvoir sp?cifi? ? la velocity actuelle
            // Ce calcul ajuste la velocity pour inclure le mouvement horizontal et vertical d?sir?
            float adjustedPower = power;

            bool isLocalTarget = playerRef == playerReference;

            playerRef.characterActor.ForceNotGrounded(5);
            playerRef.characterActor.Velocity = playerRef.characterActor.Rotation * target * adjustedPower;

            if (isLocalTarget)
            {
                MarkPushedDuringCast();
            }
        }
    }

    public void MoveTowards(GameObject monster, Vector3 target, float power)
    {
        PlayerReference player = monster.GetComponent<PlayerReference>();

        if (player != null && player.characterActor != null)
        {
            // Calcule la direction dans laquelle pousser le joueur, en s'?loignant de la cible
            Vector3 pushDirection = (player.rigidBody.position - target).normalized;
            pushDirection.y = .1f;
            player.characterActor.ForceNotGrounded();

            float adjustedPower = power;
            Vector3 pushVelocity = pushDirection * adjustedPower;

            // Applique la v?locit? directement sans tenir compte de la rotation du joueur
            player.characterActor.Velocity = pushVelocity;

            if (playerReference != null && playerReference.gameObject == monster)
            {
                MarkPushedDuringCast();
            }
        }
    }


    // PUSH LES TARGETS AU MOMEMENT DETRE TOUCHE
    public void PushTargetOverTime(PlayerReference target, Vector3 direction, float duration, float power)
    {
        float adjustedPower = power;

        if (target == playerReference)
        {
            MarkPushedDuringCast();
        }

        StartCoroutine(PushOverTimeCoroutine(target, direction, duration, adjustedPower));
    }

    private IEnumerator PushOverTimeCoroutine(PlayerReference target, Vector3 direction, float duration, float power)
    {
        float timer = 0f;

        if (target.characterActor.IsGrounded && direction.y > 0)
            target.characterActor.ForceNotGrounded();

        while (timer < duration)
        {
            target.characterActor.Velocity = direction.normalized * power;

            if (target.characterActor.IsGrounded && direction.y > 0)
                break;

            timer += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Reforce grounded si besoin
        if (direction.y <= 0 || target.characterActor.IsGrounded)
            target.characterActor.ForceGrounded();
    }






}














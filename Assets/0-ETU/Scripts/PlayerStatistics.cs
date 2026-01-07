using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SurvivorMode;
using Debug = UnityEngine.Debug;

public class PlayerStatistics : NetworkBehaviour
{
    // Liste des etats qui affectent la moveSpeed: Liste<shieldValue (additive), shieldDuration (durée), shieldTime (temps attribué)
    public List<List<float>> shieldList;


    [SerializeField, Tooltip("Bar de réincarnation")] private float reincarnationPower = 100;

    // Pour activer les effets spéciaux visuels 2D/UI
    [SerializeField] private Camera overlayCamera;

    public float baseMaxHealth;
    public bool playerInstanciated;

    ////////////////////
    /// Etats vulnérables
    [SerializeField, HideInInspector] private float stunSeconds;
    [SerializeField, HideInInspector] private float freezeSeconds;
    [SerializeField, HideInInspector] private float sleepSeconds;
    [SerializeField, HideInInspector] private float paraSeconds;
    [SerializeField, HideInInspector] private float stunAirSeconds;

    [SerializeField, HideInInspector] private float serverBlockSeconds;
    ////////////////////
    /// Etats spéciaux
    /// isBehindState: est-ce-que le joueur est dans un état de keepTargetBehind ? (s'il est avalé) 
    [HideInInspector] public PlayerReference isBehindWho;
    [HideInInspector] public Vector3 isBehindVector;

    ////////////////////////
    // Instanciations
    [SerializeField] private PlayerReference playerReference;
    [SerializeField] private GameObject damagePrefab;

    public Slider shieldBar;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider canvasHealthBar;
    [SerializeField] private GameObject canvasUser;

    [SerializeField] private cooldownUI cooldownUI;
    [SerializeField] private TextMeshProUGUI playername;

    // Le dernier attaquant qui a fait perdre des vies.
    [SerializeField] private GameObject lastAttacker;


    // Pourquoi on doit en créer un ici ? Car un client doit pouvoir avoir accès à l'info d'un autre joueur.
    // Le serveur peut utiliser playerList[clientId] de CPUcontroller.
    [SerializeField] public PlayerStruct playerDataGame;
    [SerializeField] public StatStruct playerStatData;

    [SerializeField] private bool notAttackMonsters;
    [SerializeField] private bool notAttackPlayers;
    [SerializeField] private bool notAttackHeart;

    [Header("Team")]
    [SerializeField, Range(0, 4), Tooltip("0 = aucune équipe, 1-4 = équipe. Les entités d'une même équipe s'ignorent.")]
    private int teamId;

    private Coroutine removeHealthBar;

    public bool isInvincible;

    [HideInInspector] public bool isDead = false;

    public bool isInsideZone = true;

    public float StunSeconds { get => stunSeconds; set => stunSeconds = value; }
    public float FreezeSeconds { get => freezeSeconds; set => freezeSeconds = value; }
    public float SleepSeconds { get => sleepSeconds; set => sleepSeconds = value; }
    public float ParaSeconds { get => paraSeconds; set => paraSeconds = value; }
    public float StunAirSeconds { get => stunAirSeconds; set => stunAirSeconds = value; }

    public float ServerBlockSeconds { get => serverBlockSeconds; set => serverBlockSeconds = value; }
    [SerializeField, HideInInspector]
    private float stunImmunitySeconds;
    public float StunImmunitySeconds { get => stunImmunitySeconds; set => stunImmunitySeconds = value; }
    public bool NotAttackMonsters { get => notAttackMonsters; set => notAttackMonsters = value; }
    public bool NotAttackPlayers { get => notAttackPlayers; set => notAttackPlayers = value; }
    public bool NotAttackHeart { get => notAttackHeart; set => notAttackHeart = value; }
    public float ReincarnationPower { get => reincarnationPower; set => reincarnationPower = value; }
    public int TeamId { get => Mathf.Clamp(teamId, 0, 4); set => teamId = Mathf.Clamp(value, 0, 4); }

    // Fired locally on this player object whenever health changes via RPC
    public event System.Action<float, float> OnLocalHealthChanged; // (previousHealth, currentHealth)

    [ClientRpc]
    public void UpdateStatDataClientRpc(StatStruct playerDataStruct)
    {
        if (IsLocalPlayer)
        {
            playerStatData = playerDataStruct;
            if (playerStatData.health <= 0)
            {
                playerReference.characterBrain.inputHandlerSettings.InputHandler.isDead = true;
            }
            else
            {
                playerReference.characterBrain.inputHandlerSettings.InputHandler.isDead = false;
            }
        }
    }


    public void SetAttacker(GameObject attacker)
    {
        lastAttacker = attacker;
    }

    public void RefreshName()
    {
        gameObject.name = playerDataGame.playerName.Value.ToString();
        playername.text = playerDataGame.playerName.Value.ToString();
    }

    private void Awake()
    {
        playerStatData.health = playerStatData.maxHealth;
        shieldList = new List<List<float>>();
    }


    public override void OnNetworkSpawn()
    {
        playerReference = GetComponent<PlayerReference>();

        if (IsServer)
        {
            if (gameObject.layer == 3 || gameObject.layer == 9)
            {

                if (SceneManager.GetActiveScene().name == "PlaineDeEnol")
                {
                    //NotAttackPlayers = true;
                    NotAttackHeart = true;
                }
            }
        }
        if (IsClient)
        {
            // On met à jour le slider de chaque entités côté client peu importe si c'est un monstre ou un joueur.
            // if(healthBar != null) {
            //     healthBar.maxValue = playerStatData.maxHealth;
            //     healthBar.value = playerStatData.health;
            // }
            // if(canvasHealthBar != null)
            // {
            //     canvasHealthBar.maxValue = playerStatData.maxHealth;
            //     canvasHealthBar.value = playerStatData.health;
            // }
            if (gameObject.layer == 12) return;

            if (!playerReference.playerClasses.isMonster)
            {
                if (IsLocalPlayer)
                {
                    cooldownUI.instance.GetComponentInParent<Canvas>().worldCamera = overlayCamera;
                    playerDataGame = PlayerData.player.data;
                    playerStatData = PlayerData.player.statData;
                    canvasUser.SetActive(false);
                    RefreshName();
                }
            }
        }
    }


#if UNITY_SERVER
    // Damage en dehors de la zone.
    private float timer;

    private void Update()
    {
        if (!isInsideZone)
        {
            // Seuls les joueurs prennent des dégats de zone (pas les monstres)
            if (playerReference != null && playerReference.playerClasses != null && playerReference.playerClasses.isMonster)
            {
                return;
            }
            timer += Time.deltaTime;

            if (timer >= 1.1)
            {
                ApplyDamage(gameObject, 5);
                timer = 0f;
            }
        }
    }
#endif

    /// <summary>
    /// Permet de récupérer la structure data et de la mettre dans "playerDataGame".
    /// S'il s'agit d'un client, il permet en plus de mettre à jour sa date.
    /// </summary>
    /// <param name="other"></param>
    public void SetPlayerDataLocal(PlayerStruct other)
    {
        playerDataGame = other;
        if (playerReference.networkObject.IsLocalPlayer)
        {
            PlayerData.player.data = other;
        }
        RefreshName();
    }

    public void SetStatDataLocal(StatStruct other)
    {
        playerStatData = other;
        if (playerReference.networkObject.IsLocalPlayer)
        {
            PlayerData.player.statData = other;
        }
    }

    public bool IsSameTeam(PlayerStatistics other)
    {
        if (other == null) return false;
        return TeamId > 0 && TeamId == other.TeamId;
    }

    public static bool AreAllies(PlayerReference a, PlayerReference b)
    {
        if (a == null || b == null) return false;
        return a.playerStatistics != null && b.playerStatistics != null && a.playerStatistics.IsSameTeam(b.playerStatistics);
    }

    public void ApplyDamage(GameObject player, float value)
    {
        ApplyDamage(player, value, null);
    }

    public void ApplyDamage(GameObject player, float value, PlayerReference casterRef)
    {
        PlayerReference targetRef = player != null ? player.GetComponent<PlayerReference>() : null;

        if (casterRef != null && targetRef != null && AreAllies(casterRef, targetRef))
        {
            Debug.Log("Friendly fire prevented (same team).");
            return;
        }

        if ((Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor
        || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer)
            && casterRef != null
            && !casterRef.playerClasses.isMonster
            && !(targetRef != null && targetRef.playerClasses.isMonster))
        {
            return; // on ignore le hit
        }

        bool sameGroup = casterRef != null && targetRef != null
            && casterRef.playerStatistics.playerDataGame.groupId != 0
            && casterRef.playerStatistics.playerDataGame.groupId == targetRef.playerStatistics.playerDataGame.groupId;
        if (sameGroup && (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer))
        {
            Debug.Log("Friendly fire prevented (same group).");
            return;
        }

        float totalDamage = value;
        if (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer)
        {
            float baseDamage = totalDamage; // on sauvegarde la valeur avant bonus
            float multiplier = 1f;

            if (casterRef != null && casterRef.playerClasses != null && casterRef.playerClasses.isMonster)
            {
                // Unified monster scaling (HP and Damage): per-wave +20%, every 5th wave +50%, each failed event +50%
                int wave = Run.instance.CPUcontroller.zone.currentWave;
            }
            else if (casterRef != null && casterRef.PlayerReincarnation != null && casterRef.PlayerReincarnation.IsReincarnation)
            {
                multiplier *= SurvivorModifiers.spiritDamageMultiplier;
            }
            else if (casterRef != null)
            {
                multiplier *= SurvivorModifiers.humanDamageMultiplier;
            }
            totalDamage = baseDamage * multiplier;
        }
        // Pacifist challenge: if any player damages a monster, fail immediately (server-side)
        if (casterRef != null && SurvivorModifiers.pacifist30sChallengeThisWave)
        {
            if (targetRef != null && targetRef.playerClasses != null && targetRef.playerClasses.isMonster && !casterRef.playerClasses.isMonster)
            {
                SurvivorEventsApi.Instance?.ReportChallengeFailed();
            }
        }
        if (shieldList.Count > 0)
        {
            int shieldIndex = 0;
            foreach (List<float> shield in shieldList.ToList())
            {
                if (shield[0] > 0)
                {
                    shieldList[shieldIndex][0] = totalDamage > shield[0] ? 0 : shield[0] - totalDamage;

                    if (totalDamage < shield[0])
                    {
                        totalDamage = 0;
                        break;
                    }
                    else
                    {
                        totalDamage -= shield[0];
                    }
                }
                shieldIndex++;
            }
        }

        if (totalDamage > 0)
        {
            playerStatData.health -= totalDamage;

            if (casterRef == null)
            {
                OnAutoHealthClientRpc(player, playerStatData);
            }
            else
            {
                OnHealthChangedClientRpc(player, playerStatData, casterRef.gameObject, totalDamage);
            }

            ApplyDamageServer(player, playerStatData.health);
        }
    }

    public void ApplyHeal(GameObject player, float value)
    {
        ApplyHeal(player, value, null);
    }

    public void ApplyHeal(GameObject player, float value, PlayerReference healerRef)
    {
        float totalHeal = Mathf.Max(0f, value);
        if (totalHeal <= 0f)
            return;

        float newHealth = Mathf.Min(playerStatData.maxHealth, playerStatData.health + totalHeal);
        playerStatData.health = newHealth;

        // No attacker context for heal; update UI/state across clients
        OnAutoHealthClientRpc(player, playerStatData);
    }

    private void ApplyDamageServer(GameObject player, float newValue)
    {
#if UNITY_SERVER
        // Vérification des vies :
        if (newValue <= 0 && !isDead)
        {
            Debug.Log("IsDead PlayerStatistics: " + playerReference.gameObject.name);
            // Coeur (layer 12)
            if (player.layer == 12)
            {
                if (SurvivorModifiers.defendStoneHeartThisWave)
                {
                    SurvivorEventsApi.Instance?.ReportChallengeFailed();
                }

                StoneHeartTarget.Instance?.HandleHeartDestroyed();
                return;
            }

            if (playerReference.characterBrain != null)
                playerReference.characterBrain.inputHandlerSettings.InputHandler.isDead = true;

            isDead = true;
            // Record the killer's kill before marking the victim as dead,
            // so end-of-match save counts the final kill in BR.
            if (!playerReference.playerClasses.isMonster)
            {
                if (lastAttacker != null)
                {
                    PlayerStatistics statsPl = lastAttacker.GetComponent<PlayerStatistics>();
                    statsPl.RecordKill(false);
                    UpdateStatDataClientRpc(statsPl.playerStatData);
                }
            }

            if (playerReference.dropSystem != null)
            {
                PlayerReference deserveFor = null;
                if (lastAttacker != null)
                {
                    deserveFor = lastAttacker.GetComponent<PlayerReference>();
                }
                playerReference.dropSystem.DropItNow(deserveFor);
            }

            if (!playerReference.playerClasses.isMonster)
            {
                playerDataGame.lose += 1;
                UpdateStatDataClientRpc(playerStatData);
            }

            if (!playerReference.playerClasses.isMonster && !(player.layer == 12))
            {
                // Debug.Log("Déconnexion du joueur : " + this);
            }

            if (playerReference.playerClasses.isMonster || player.layer == 12)
            {
                if (lastAttacker != null)
                {
                    PlayerStatistics statsPl = lastAttacker.GetComponent<PlayerStatistics>();
                    statsPl.RecordKill(true);
                }
            }
            // Defend Stone Heart challenge: if heart (layer 12) destroyed, fail the challenge
            if (player.layer == 12 && SurvivorModifiers.defendStoneHeartThisWave)
            {
                SurvivorEventsApi.Instance?.ReportChallengeFailed();
            }
        }
#endif
    }



    [ClientRpc]
    public void OnHealthChangedClientRpc(NetworkObjectReference playerRef, StatStruct statData, NetworkObjectReference attackerRef, float totalDamage)
    {
        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            PlayerReference playerRefer = playerNet.GetComponent<PlayerReference>();
            // Track previous health before applying new stats
            float prevHealth = playerRefer.playerStatistics.playerStatData.health;
            DealDamageWith(playerRefer, statData);
            // Notify local listeners (VFX, etc.)
            try { playerRefer.playerStatistics.OnLocalHealthChanged?.Invoke(prevHealth, statData.health); } catch { }

            if (attackerRef.TryGet(out NetworkObject attackerNet))
            {
                PlayerReference killerRef = attackerNet.GetComponent<PlayerReference>();
                if (attackerNet.IsLocalPlayer)
                {
                    cooldownUI.instance.UpdateEnemyIndicator(playerRefer);
                    DamagePrefab component = Instantiate(cooldownUI.instance.damagePrefab).GetComponent<DamagePrefab>();
                    component.SetFollow(playerRefer.transform);
                    component.SetText(statData.health > 0f ? -totalDamage : 666);
                    cooldownUI.instance.PlayCursorHitAnimation();
                    if (statData.health > 0f)
                    {
                        cooldownUI.instance.UpdateEnemyIndicator(playerRefer);
                        SoundManager.Instance.Play2D("hit");
                    }
                    else
                    {
                        killerRef.impulseSource?.GenerateImpulse();
                        if (playerRefer.playerClasses.isMonster)
                        {
                            SoundManager.Instance.Play2D("monster-kill");
                            cooldownUI.instance.feedbackEnemy.StopIt();
                        }
                        else
                        {
                            cooldownUI.instance.feedbackEnemy.PlayKillEffect();
                            cooldownUI.instance.feedbackEnemy.UpdateKillEffect(playerRefer.gameObject.name, playerRefer.playerStatistics.playerDataGame.avatarId);
                            Run.instance.CPUcontroller.zone.PlayZoneAudio(3);
                        }
                    }

                }
                else
                {
                    if (playerNet.IsLocalPlayer)
                    {
                        cooldownUI.instance.UpdateEnemyIndicator(killerRef);
                        cooldownUI.instance.PlayHitEffect();
                        playerRefer.impulseSource?.GenerateImpulse();
                        SoundManager.Instance.Play2D("hitted");

                        try
                        {
                            float maxH = Mathf.Max(1f, playerRefer.playerStatistics.playerStatData.maxHealth);
                            float intensity = Mathf.Clamp01(totalDamage / maxH);
                            var hook = playerRefer.playerDamageHook;
                            if (hook != null)
                            {
                                Debug.Log("On instantiate le hook (Tomek)");
                                hook.OnDamagedBySourcePosition(killerRef.transform.position, intensity);
                            }
                            else if (DamageDirectionUI.instance != null)
                            {
                                DamageDirectionUI.instance.ShowDamageFromWorld(killerRef.transform.position, intensity);
                            }
                        }
                        catch { }

                        if (statData.health <= 0f)
                        {
                            if (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer)
                            {
                                Debug.Log("Etat mort");
                            }
                        }
                    }
                }

                if (statData.health < 0f && !killerRef.playerClasses.isMonster && !playerRefer.playerClasses.isMonster)
                {
                    SoundManager.Instance.Play2D("hitted");
                    KillResume killResume = Instantiate(cooldownUI.instance.killResumePrefab, cooldownUI.instance.killResumeContainer).GetComponent<KillResume>();
                    killResume.SetKillResume(killerRef, playerRefer);
                }
            }
        }
    }

    [ClientRpc]
    public void OnAutoHealthClientRpc(NetworkObjectReference playerRef, StatStruct statData)
    {

        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            PlayerReference playerRefer = playerNet.GetComponent<PlayerReference>();
            DealDamageWith(playerRefer, statData);
        }
    }

    private void DealDamageWith(PlayerReference playerRefer, StatStruct statData)
    {
        // Si ce n'est pas le joueur local, on met à jour la barre de vie au dessus des autres joueurs
        // (nous n'avons pas de barre, nous)

        playerRefer.playerStatistics.playerStatData = statData;



        // playerRefer.playerStatistics.healthBar.value = statData.health > 0 ? statData.health : 0;


        // if (playerRefer.playerStatistics.healthBar != null)
        // {
        //     if (!playerRefer.IsLocalPlayer)
        //     {
        //         playerRefer.playerStatistics.healthBar.gameObject.SetActive(true);
        //         if (removeHealthBar != null) StopCoroutine(removeHealthBar);
        //         removeHealthBar = StartCoroutine(HideCanvasHealthBar(playerRefer.playerStatistics));
        //     }
        //     else{
        //         playerRefer.impulseSource.GenerateImpulse();
        //     }
        // }
        // if (playerRefer.playerStatistics.canvasHealthBar != null)
        // {
        //     playerRefer.playerStatistics.canvasHealthBar.value = statData.health;
        // }
        if (playerRefer.playerClasses != null)
        {
            if (!playerRefer.playerClasses.isMonster && !(gameObject.layer == 12))
            {
                cooldownUI.instance.UpdateUiInformation();
            }
        }

        if (IsClient)
        {
            if (playerRefer.effectUtils != null && statData.health <= 0f && !playerRefer.effectUtils.isDead)
            {
                playerRefer.effectUtils.HideDissolve(3.0f);
            }
            else
            {
                playerRefer.effectUtils.HitEffect(0.25f);
            }
        }
    }

    // RPC A ENVOYER A TOUT LE MONDE
    [ClientRpc]
    public void UpdateUiAndPlayerDataClientRpc(NetworkObjectReference defenderRef, NetworkObjectReference attackerRef, ClientRpcParams clientParams = default)
    {
        cooldownUI.instance.UpdateUiInformation();
        if (attackerRef.TryGet(out NetworkObject attackerNet))
        {
            if (!attackerNet.IsLocalPlayer)
            {
                cooldownUI.instance.UpdateEnemyIndicator(attackerNet.GetComponent<PlayerReference>());
            }
        }
    }

    /*[ClientRpc]
    void RespawnClientRpc(Vector3 position)
    {
        // Ici le serveur demande aux clients d'executer cette fonction:
        StartCoroutine(Respawn(position));
    }


    IEnumerator Respawn(Vector3 position)
    {
        // Cela doit détruire le fonctionnement de la synchro des mouvements multijoueurs !

        yield return new WaitForSeconds(3f);
        cc.enabled = false;
        transform.position = position;
        cc.enabled = true;
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }
    }*/

    public int GetKills()
    {
        return playerStatData.kill;
    }

    public int GetPoints()
    {
        return 0;
        //return SceneManager.GetActiveScene().name != "PlaineDeEnol" ? playerDataGame.point : playerDataGame.wavePoint;
    }
    public string GetAccountId()
    {
        return playerDataGame.accountId.Value.ToString();
    }


    [ClientRpc]
    public void UpdateSoulHealthClientRpc(StatStruct statPl)
    {
        if (playerStatData.soul != statPl.soul)
        {
            if (IsLocalPlayer)
            {
                cooldownUI.instance.PlaySoulEffect();
            }
        }
        playerStatData.soul = statPl.soul;
        playerStatData.maxHealth = statPl.maxHealth;
        playerStatData.health = statPl.health;
        SoundManager.Instance.Play2D("soul");
        cooldownUI.instance.UpdateUiInformation();
    }

    [ClientRpc]
    public void ApplyStunClientRpc(float stun, float freeze, float sleep, float para, float stunAir, ClientRpcParams clientRpcParams = default)
    {
        StunSeconds = stun;
        FreezeSeconds = freeze;
        SleepSeconds = sleep;
        ParaSeconds = para;
        StunAirSeconds = stunAir;

        if (!playerReference.playerClasses.isMonster)
        {
            var input = playerReference.characterBrain.inputHandlerSettings.InputHandler;
            if (input != null)
            {
                input.isStunned = stun > 0f;
                input.isFrozen = freeze > 0f;
                input.isSleeping = sleep > 0f;
                input.isParalyzed = para > 0f;
                input.isStunnedAir = stunAir > 0f;
            }
        }
    }

    public void RecordKill(bool isMonster)
    {

        if (isMonster)
        {
            playerStatData.monsterKills += 1;
            UpdateStatDataClientRpc(playerStatData);
        }
        else
        {
            playerStatData.kill += 1;
            playerStatData.soul += 10;
            playerStatData.health += 15;
            UpdateStatDataClientRpc(playerStatData);
        }
    }
}




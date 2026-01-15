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
    // Liste des etats qui affectent la moveSpeed
    public List<List<float>> shieldList;

    [SerializeField, Tooltip("Bar de réincarnation")] private float reincarnationPower = 100;
    [SerializeField] private Camera overlayCamera;

    public float baseMaxHealth;
    public bool playerInstanciated;

    // Etats vulnérables
    [SerializeField, HideInInspector] private float stunSeconds;
    [SerializeField, HideInInspector] private float freezeSeconds;
    [SerializeField, HideInInspector] private float sleepSeconds;
    [SerializeField, HideInInspector] private float paraSeconds;
    [SerializeField, HideInInspector] private float stunAirSeconds;
    [SerializeField, HideInInspector] private float serverBlockSeconds;

    // Etats spéciaux
    [HideInInspector] public PlayerReference isBehindWho;
    [HideInInspector] public Vector3 isBehindVector;

    // Instanciations
    [SerializeField] private PlayerReference playerReference;
    [SerializeField] private GameObject damagePrefab;

    public Slider shieldBar;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider canvasHealthBar;
    [SerializeField] private GameObject canvasUser;

    [SerializeField] private cooldownUI cooldownUI;
    [SerializeField] private TextMeshProUGUI playername;

    [SerializeField] private GameObject lastAttacker;

    [SerializeField] public PlayerStruct playerDataGame;
    [SerializeField] public StatStruct playerStatData;

    [SerializeField] private bool notAttackMonsters;
    [SerializeField] private bool notAttackPlayers;
    [SerializeField] private bool notAttackHeart;

    [Header("Team")]
    [SerializeField, Range(0, 4)] private int teamId;

    private Coroutine removeHealthBar;
    public bool isInvincible;
    [HideInInspector] public bool isDead = false;
    public bool isInsideZone = true;

    // Properties
    public float StunSeconds { get => stunSeconds; set => stunSeconds = value; }
    public float FreezeSeconds { get => freezeSeconds; set => freezeSeconds = value; }
    public float SleepSeconds { get => sleepSeconds; set => sleepSeconds = value; }
    public float ParaSeconds { get => paraSeconds; set => paraSeconds = value; }
    public float StunAirSeconds { get => stunAirSeconds; set => stunAirSeconds = value; }
    public float ServerBlockSeconds { get => serverBlockSeconds; set => serverBlockSeconds = value; }
    
    [SerializeField, HideInInspector] private float stunImmunitySeconds;
    public float StunImmunitySeconds { get => stunImmunitySeconds; set => stunImmunitySeconds = value; }
    
    public bool NotAttackMonsters { get => notAttackMonsters; set => notAttackMonsters = value; }
    public bool NotAttackPlayers { get => notAttackPlayers; set => notAttackPlayers = value; }
    public bool NotAttackHeart { get => notAttackHeart; set => notAttackHeart = value; }
    public float ReincarnationPower { get => reincarnationPower; set => reincarnationPower = value; }
    public int TeamId { get => Mathf.Clamp(teamId, 0, 4); set => teamId = Mathf.Clamp(value, 0, 4); }

    public event System.Action<float, float> OnLocalHealthChanged;

    private void Awake()
    {
        // FIX : Initialisation des PV par défaut si oubli (évite mort instantanée)
        if (playerStatData.maxHealth <= 0) playerStatData.maxHealth = 100f;
        playerStatData.health = playerStatData.maxHealth;
        shieldList = new List<List<float>>();
        isDead = false;
    }

    public override void OnNetworkSpawn()
    {
        // FIX 1 : On s'assure d'avoir la référence
        if (playerReference == null) playerReference = GetComponent<PlayerReference>();

        if (IsServer)
        {
            if (gameObject.layer == 3 || gameObject.layer == 9)
            {
                if (SceneManager.GetActiveScene().name == "PlaineDeEnol")
                {
                    NotAttackHeart = true;
                }
            }
        }
        
        if (IsClient)
        {
            if (gameObject.layer == 12) return;

            // FIX 2 : SÉCURITÉ CRITIQUE
            // On vérifie d'abord si playerClasses existe. 
            // Si c'est un bot simple, playerClasses sera null, donc on saute ce bloc au lieu de planter.
            bool hasClasses = (playerReference != null && playerReference.playerClasses != null);
            
            if (hasClasses && !playerReference.playerClasses.isMonster)
            {
                if (IsLocalPlayer)
                {
                    // FIX 3 : Sécurité UI
                    if (cooldownUI.instance != null)
                    {
                        var canvas = cooldownUI.instance.GetComponentInParent<Canvas>();
                        if (canvas != null) canvas.worldCamera = overlayCamera;
                    }
                    
                    if (PlayerData.player != null)
                    {
                        playerDataGame = PlayerData.player.data;
                        playerStatData = PlayerData.player.statData;
                    }
                    
                    if (canvasUser != null) canvasUser.SetActive(false);
                    RefreshName();
                }
            }
            
            // Mise à jour UI santé (si assignée)
            if(healthBar != null) {
               healthBar.maxValue = playerStatData.maxHealth;
               healthBar.value = playerStatData.health;
            }
            if(canvasHealthBar != null)
            {
               canvasHealthBar.maxValue = playerStatData.maxHealth;
               canvasHealthBar.value = playerStatData.health;
            }
        }
    }

    [ClientRpc]
    public void UpdateStatDataClientRpc(StatStruct playerDataStruct)
    {
        if (IsLocalPlayer)
        {
            playerStatData = playerDataStruct;
            // Sécurité Brain
            if (playerReference.characterBrain != null && playerReference.characterBrain.inputHandlerSettings.InputHandler != null)
            {
                playerReference.characterBrain.inputHandlerSettings.InputHandler.isDead = (playerStatData.health <= 0);
            }
        }
    }

    public void SetAttacker(GameObject attacker)
    {
        lastAttacker = attacker;
    }

    public void RefreshName()
    {
        if(playerDataGame.playerName.Value != null) 
        {
            gameObject.name = playerDataGame.playerName.Value.ToString();
            if(playername != null) playername.text = playerDataGame.playerName.Value.ToString();
        }
    }

#if UNITY_SERVER
    private float timer;
    private void Update()
    {
        if (!isInsideZone)
        {
            // Seuls les joueurs prennent des dégats de zone (pas les monstres)
            // FIX : Check null sur playerClasses
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

    public void SetPlayerDataLocal(PlayerStruct other)
    {
        playerDataGame = other;
        if (playerReference.networkObject.IsLocalPlayer && PlayerData.player != null)
        {
            PlayerData.player.data = other;
        }
        RefreshName();
    }

    public void SetStatDataLocal(StatStruct other)
    {
        playerStatData = other;
        if (playerReference.networkObject.IsLocalPlayer && PlayerData.player != null)
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

        if (player == null) return;
            NetworkObject targetNetParams = player.GetComponent<NetworkObject>();
            if (targetNetParams == null || !targetNetParams.IsSpawned) 
            {
            // On ne peut pas appliquer de dégâts à un objet qui n'existe pas sur le réseau
            return;
        }

        if (casterRef != null)
        {
        if (casterRef.networkObject == null || !casterRef.networkObject.IsSpawned)
            {
            // L'attaquant n'est pas valide réseau, on annule pour éviter le crash
            return;
            }
         }   

        if (casterRef != null && targetRef != null && AreAllies(casterRef, targetRef))
        {
            return;
        }

        if (Run.instance != null && Run.instance.CPUcontroller != null) 
        {
            var mode = Run.instance.CPUcontroller.zone.serverMode;
            if ((mode == GameMode.Survivor || mode == GameMode.Streamer)
                && casterRef != null
                && casterRef.playerClasses != null && !casterRef.playerClasses.isMonster
                && !(targetRef != null && targetRef.playerClasses != null && targetRef.playerClasses.isMonster))
            {
                return; // PvP désactivé en Survivor
            }

            bool sameGroup = casterRef != null && targetRef != null
                && casterRef.playerStatistics.playerDataGame.groupId != 0
                && casterRef.playerStatistics.playerDataGame.groupId == targetRef.playerStatistics.playerDataGame.groupId;
            
            if (sameGroup && (mode == GameMode.Survivor || mode == GameMode.Streamer))
            {
                return;
            }
        }

        float totalDamage = value;
        
        // Calcul multiplicateurs de dégâts
        if (Run.instance != null && (Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor || Run.instance.CPUcontroller.zone.serverMode == GameMode.Streamer))
        {
            float baseDamage = totalDamage;
            float multiplier = 1f;

            if (casterRef != null && casterRef.playerClasses != null && casterRef.playerClasses.isMonster)
            {
                // Logique scaling monstre
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

        // Challenge Pacifiste
        if (casterRef != null && SurvivorModifiers.pacifist30sChallengeThisWave)
        {
            if (targetRef != null && targetRef.playerClasses != null && targetRef.playerClasses.isMonster && casterRef.playerClasses != null && !casterRef.playerClasses.isMonster)
            {
                SurvivorEventsApi.Instance?.ReportChallengeFailed();
            }
        }

        // Gestion Boucliers
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

        // Application Dégâts
        if (totalDamage > 0)
        {
            playerStatData.health -= totalDamage;

            if (casterRef == null)
                OnAutoHealthClientRpc(player, playerStatData);
            else
                OnHealthChangedClientRpc(player, playerStatData, casterRef.gameObject, totalDamage);

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
        if (totalHeal <= 0f) return;

        float newHealth = Mathf.Min(playerStatData.maxHealth, playerStatData.health + totalHeal);
        playerStatData.health = newHealth;

        OnAutoHealthClientRpc(player, playerStatData);
    }

    private void ApplyDamageServer(GameObject player, float newValue)
    {
#if UNITY_SERVER
        if (newValue <= 0 && !isDead)
        {
            // Coeur (layer 12)
            if (player.layer == 12)
            {
                if (SurvivorModifiers.defendStoneHeartThisWave)
                    SurvivorEventsApi.Instance?.ReportChallengeFailed();

                StoneHeartTarget.Instance?.HandleHeartDestroyed();
                return;
            }

            if (playerReference.characterBrain != null)
                playerReference.characterBrain.inputHandlerSettings.InputHandler.isDead = true;

            isDead = true;

            // FIX : Check null sur playerClasses pour éviter crash bot
            if (playerReference.playerClasses != null && !playerReference.playerClasses.isMonster)
            {
                if (lastAttacker != null)
                {
                    PlayerStatistics statsPl = lastAttacker.GetComponent<PlayerStatistics>();
                    if(statsPl != null)
                    {
                        statsPl.RecordKill(false);
                        UpdateStatDataClientRpc(statsPl.playerStatData);
                    }
                }
            }

            if (playerReference.dropSystem != null)
            {
                PlayerReference deserveFor = null;
                if (lastAttacker != null) deserveFor = lastAttacker.GetComponent<PlayerReference>();
                playerReference.dropSystem.DropItNow(deserveFor);
            }

            if (playerReference.playerClasses != null && !playerReference.playerClasses.isMonster)
            {
                playerDataGame.lose += 1;
                UpdateStatDataClientRpc(playerStatData);
            }

            if ((playerReference.playerClasses != null && playerReference.playerClasses.isMonster) || player.layer == 12)
            {
                if (lastAttacker != null)
                {
                    PlayerStatistics statsPl = lastAttacker.GetComponent<PlayerStatistics>();
                    if(statsPl != null) statsPl.RecordKill(true);
                }
            }
            
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
            float prevHealth = playerRefer.playerStatistics.playerStatData.health;
            DealDamageWith(playerRefer, statData);
            
            try { playerRefer.playerStatistics.OnLocalHealthChanged?.Invoke(prevHealth, statData.health); } catch { }

            if (attackerRef.TryGet(out NetworkObject attackerNet))
            {
                PlayerReference killerRef = attackerNet.GetComponent<PlayerReference>();
                
                // FIX: Vérif cooldownUI avant d'utiliser
                if (attackerNet.IsLocalPlayer && cooldownUI.instance != null)
                {
                    cooldownUI.instance.UpdateEnemyIndicator(playerRefer);
                    
                    if(cooldownUI.instance.damagePrefab != null) {
                        DamagePrefab component = Instantiate(cooldownUI.instance.damagePrefab).GetComponent<DamagePrefab>();
                        component.SetFollow(playerRefer.transform);
                        component.SetText(statData.health > 0f ? -totalDamage : 666);
                    }
                    
                    cooldownUI.instance.PlayCursorHitAnimation();
                    if (statData.health > 0f)
                    {
                        cooldownUI.instance.UpdateEnemyIndicator(playerRefer);
                        SoundManager.Instance.Play2D("hit");
                    }
                    else
                    {
                        if(killerRef.impulseSource != null) killerRef.impulseSource.GenerateImpulse();
                        
                        if (playerRefer.playerClasses != null && playerRefer.playerClasses.isMonster)
                        {
                            SoundManager.Instance.Play2D("monster-kill");
                            if(cooldownUI.instance.feedbackEnemy != null) cooldownUI.instance.feedbackEnemy.StopIt();
                        }
                        else
                        {
                            if(cooldownUI.instance.feedbackEnemy != null) {
                                cooldownUI.instance.feedbackEnemy.PlayKillEffect();
                                cooldownUI.instance.feedbackEnemy.UpdateKillEffect(playerRefer.gameObject.name, playerRefer.playerStatistics.playerDataGame.avatarId);
                            }
                            if(Run.instance != null) Run.instance.CPUcontroller.zone.PlayZoneAudio(3);
                        }
                    }
                }
                else
                {
                    if (playerNet.IsLocalPlayer && cooldownUI.instance != null)
                    {
                        cooldownUI.instance.UpdateEnemyIndicator(killerRef);
                        cooldownUI.instance.PlayHitEffect();
                        if(playerRefer.impulseSource != null) playerRefer.impulseSource.GenerateImpulse();
                        SoundManager.Instance.Play2D("hitted");

                        try
                        {
                            float maxH = Mathf.Max(1f, playerRefer.playerStatistics.playerStatData.maxHealth);
                            float intensity = Mathf.Clamp01(totalDamage / maxH);
                            var hook = playerRefer.playerDamageHook;
                            if (hook != null)
                            {
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

                bool killerIsMonster = (killerRef.playerClasses != null && killerRef.playerClasses.isMonster);
                bool victimIsMonster = (playerRefer.playerClasses != null && playerRefer.playerClasses.isMonster);

                if (statData.health < 0f && !killerIsMonster && !victimIsMonster)
                {
                    SoundManager.Instance.Play2D("hitted");
                    if(cooldownUI.instance != null && cooldownUI.instance.killResumePrefab != null) {
                        KillResume killResume = Instantiate(cooldownUI.instance.killResumePrefab, cooldownUI.instance.killResumeContainer).GetComponent<KillResume>();
                        killResume.SetKillResume(killerRef, playerRefer);
                    }
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
        playerRefer.playerStatistics.playerStatData = statData;

        if (playerRefer.playerClasses != null)
        {
            if (!playerRefer.playerClasses.isMonster && !(gameObject.layer == 12))
            {
                if(cooldownUI.instance != null) cooldownUI.instance.UpdateUiInformation();
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
                if(playerRefer.effectUtils != null) playerRefer.effectUtils.HitEffect(0.25f);
            }
        }
    }

    [ClientRpc]
    public void UpdateUiAndPlayerDataClientRpc(NetworkObjectReference defenderRef, NetworkObjectReference attackerRef, ClientRpcParams clientParams = default)
    {
        if(cooldownUI.instance != null) cooldownUI.instance.UpdateUiInformation();
        if (attackerRef.TryGet(out NetworkObject attackerNet))
        {
            if (!attackerNet.IsLocalPlayer)
            {
                if(cooldownUI.instance != null) cooldownUI.instance.UpdateEnemyIndicator(attackerNet.GetComponent<PlayerReference>());
            }
        }
    }

    public int GetKills()
    {
        return playerStatData.kill;
    }

    public int GetPoints()
    {
        return 0;
    }
    public string GetAccountId()
    {
        if(playerDataGame.accountId.Value == null) return "0";
        return playerDataGame.accountId.Value.ToString();
    }

    [ClientRpc]
    public void UpdateSoulHealthClientRpc(StatStruct statPl)
    {
        if (playerStatData.soul != statPl.soul)
        {
            if (IsLocalPlayer && cooldownUI.instance != null)
            {
                cooldownUI.instance.PlaySoulEffect();
            }
        }
        playerStatData.soul = statPl.soul;
        playerStatData.maxHealth = statPl.maxHealth;
        playerStatData.health = statPl.health;
        SoundManager.Instance.Play2D("soul");
        if(cooldownUI.instance != null) cooldownUI.instance.UpdateUiInformation();
    }

    [ClientRpc]
    public void ApplyStunClientRpc(float stun, float freeze, float sleep, float para, float stunAir, ClientRpcParams clientRpcParams = default)
    {
        StunSeconds = stun;
        FreezeSeconds = freeze;
        SleepSeconds = sleep;
        ParaSeconds = para;
        StunAirSeconds = stunAir;

        // FIX : Sécurité si PlayerClasses n'existe pas
        if (playerReference.playerClasses != null && !playerReference.playerClasses.isMonster)
        {
            if(playerReference.characterBrain != null) {
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
    }

    // UNIQUE FONCTION RECORDKILL
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
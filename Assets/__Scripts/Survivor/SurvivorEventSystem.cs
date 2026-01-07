using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using SurvivorMode.Events;
using Json;

namespace SurvivorMode
{
    // Server-authoritative event system for Survivor mode.
    // Triggers an event every 5th wave and reverts it at wave end.
    public class SurvivorEventSystem : NetworkBehaviour
    {
        public static SurvivorEventSystem Instance { get; private set; }

        [Header("Event Pool")]
        [SerializeField] private List<SurvivorEvent> availableEvents = new List<SurvivorEvent>();

        [Header("Behaviour")]
        [Tooltip("If enabled, polls Run.instance.CPUcontroller.zone to auto-detect waves in Survivor mode.")]
        [SerializeField] private bool autoDetectFromRun = false;
        [SerializeField] private float pollInterval = 0.5f;
        private SurvivorEvent activeEvent;
        private NetworkObject spawnedStoneHeart;
        private int spawnedHeartSpawnPointIndex = -1;
        private int lastObservedWave = -1;

        // UI/UX hooks (client-side can subscribe)
        public static event Action<string, string> OnEventStartedClient; // title, description
        public static event Action<int, int, string, string> OnEventStartedClientKeys; // titleKey, descKey, imageKey, audioKey
        public static event Action OnEventEndedClient;
        public static event Action<bool, int, int, string, string> OnChallengeOutcomeClient; // success?, titleKey, descKey, imageKey, audioKey
        public static event Action<NetworkObjectReference> OnStoneHeartAnnouncedClient; // heart ref (client-only UI)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }



        private void OnEnable()
        {
            StoneHeartTarget.OnHeartDestroyedServer += HandleStoneHeartDestroyed;
        }

        private void OnDisable()
        {
            StoneHeartTarget.OnHeartDestroyedServer -= HandleStoneHeartDestroyed;
        }
        public override void OnNetworkSpawn()
        {
            if (IsServer && autoDetectFromRun)
            {
                Debug.Log("[SurvivorEventSystem] Online");
                InvokeRepeating(nameof(PollRunForWave), pollInterval, pollInterval);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && autoDetectFromRun)
            {
                CancelInvoke(nameof(PollRunForWave));
            }
        }

        // Manual integration API – call these from your wave system
        public void NotifyWaveStarted(int waveNumber)
        {
            if (!IsServer) return;
            Debug.Log("NotifyWaveStarted");
            TryTriggerEventForWave(waveNumber);
            BroadcastCurrentMonsterBonus();
        }

        public void NotifyWaveEnded(int waveNumber)
        {
            if (!IsServer) return;
            Debug.Log("NotifyWaveEnded");
            EndActiveEvent();
            BroadcastCurrentMonsterBonus();
        }

        // Core: trigger every 5 waves in Survivor mode
        private void TryTriggerEventForWave(int waveNumber)
        {
            if (!IsCurrentModeSurvivor()) return;
            if (waveNumber <= 0) return;
            if (waveNumber % 5 != 0) return;

            // Reset previous modifiers and event
            EndActiveEvent();
            SurvivorModifiers.ResetPerWave();

            var evt = PickRandomEvent();
            if (evt == null)
            {
                Debug.LogWarning("[SurvivorEventSystem] No available event to trigger.");
                return;
            }
            Debug.Log("active Event: " + evt.name);
            activeEvent = evt;
            try
            {
                activeEvent.Apply();
                // Apply immediate per-wave effects
                ApplyPerWaveEffectsAfterEventApplied();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurvivorEventSystem] Error applying event {activeEvent.name}: {ex}");
                activeEvent = null;
                return;
            }

            // Notify clients
            BroadcastEventClientRpc(activeEvent.title, activeEvent.description);
            BroadcastEventKeysClientRpc(activeEvent.titleKey, activeEvent.descriptionKey, activeEvent.imageResourceKey, activeEvent.audioKey);
            Debug.Log($"[SurvivorEventSystem] Event started: {activeEvent.title}");
            BroadcastCurrentMonsterBonus();
        }

        private void EndActiveEvent()
        {
            if (!IsServer) return;
            if (activeEvent == null) return;

            try
            {
                activeEvent.Revert();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurvivorEventSystem] Error reverting event {activeEvent.name}: {ex}");
            }
            finally
            {
                activeEvent = null;
                SurvivorModifiers.ResetPerWave();
                // Despawn any spawned stone heart for this wave
                try
                {
                    if (spawnedStoneHeart != null)
                    {
                        var zone = Run.instance?.CPUcontroller?.zone;
                        if (zone != null && spawnedHeartSpawnPointIndex >= 0 && spawnedHeartSpawnPointIndex < zone.playerSpawnPoints.Count)
                        {
                            zone.playerSpawnPoints[spawnedHeartSpawnPointIndex].isOccupied = false;
                        }
                        if (spawnedStoneHeart.IsSpawned)
                            spawnedStoneHeart.Despawn();
                        Destroy(spawnedStoneHeart.gameObject);
                        spawnedStoneHeart = null;
                        spawnedHeartSpawnPointIndex = -1;
                        Debug.Log("[SurvivorEventSystem] Stone Heart despawned.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SurvivorEventSystem] Failed to despawn Stone Heart: " + e.Message);
                }
                BroadcastCurrentMonsterBonus();
                // Restore base HP (multiplier now reset to 1)
                try
                {
                    List<PlayerReference> targets = new List<PlayerReference>();

                    foreach (var p in targets)
                    {
                        if (p == null || p.playerStatistics == null) continue;
                        if (p.playerClasses != null && p.playerClasses.isMonster) continue;

                        var stats = p.playerStatistics.playerStatData;
                        string playerName = p.gameObject != null ? p.gameObject.name : "<unknown>";
                        float oldMax = stats.maxHealth;
                        float oldHealth = stats.health;
                        float baseMax = p.playerStatistics.baseMaxHealth > 0f ? p.playerStatistics.baseMaxHealth : stats.maxHealth;
                        float newMax = Mathf.Max(1f, baseMax * SurvivorModifiers.playerMaxHealthMultiplier);
                        stats.maxHealth = newMax;
                        stats.health = Mathf.Min(stats.health, newMax);
                        p.playerStatistics.playerStatData = stats;
                        Debug.Log($"[SurvivorEventSystem] (End) HP update for {playerName}: Max {oldMax} -> {stats.maxHealth}, Health {oldHealth} -> {stats.health} (multiplier={SurvivorModifiers.playerMaxHealthMultiplier})");
                        p.playerStatistics.UpdateSoulHealthClientRpc(stats);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SurvivorEventSystem] Failed to restore HP after event: " + e.Message);
                }
                NotifyEventEndedClientRpc();
                Debug.Log("[SurvivorEventSystem] Event ended and modifiers reset.");
            }
        }

        // Systems can call these when a challenge outcome is known (server-side)
        public void ReportChallengeFailed()
        {
            if (!IsServer) return;
            if (activeEvent == null) return;
            try { activeEvent.OnChallengeFailed(); }
            catch (Exception ex) { Debug.LogWarning("[SurvivorEventSystem] OnChallengeFailed error: " + ex.Message); }
            BroadcastOutcomeClientRpc(false, activeEvent.failureTitleKey, activeEvent.failureDescriptionKey, activeEvent.failureImageResourceKey, activeEvent.failureAudioKey);

            // Increase monster bonus on failure (+10%) and notify clients
            SurvivorModifiers.failedEventCount = Mathf.Max(0, SurvivorModifiers.failedEventCount) + 1;
            BroadcastCurrentMonsterBonus();
        }

        public void ReportChallengeSucceeded()
        {
            if (!IsServer) return;
            if (activeEvent == null) return;
            try { activeEvent.OnChallengeSucceeded(); }
            catch (Exception ex) { Debug.LogWarning("[SurvivorEventSystem] OnChallengeSucceeded error: " + ex.Message); }
            BroadcastOutcomeClientRpc(true, activeEvent.successTitleKey, activeEvent.successDescriptionKey, activeEvent.successImageResourceKey, activeEvent.successAudioKey);
        }

        [ClientRpc]
        private void BroadcastOutcomeClientRpc(bool success, int titleKey, int descriptionKey, string imageKey, string audioKey)
        {
            OnChallengeOutcomeClient?.Invoke(success, titleKey, descriptionKey, imageKey, audioKey);
        }

        private SurvivorEvent PickRandomEvent()
        {
            var pool = new List<SurvivorEvent>();
            foreach (var e in availableEvents)
            {
                if (e != null && e.IsAvailable()) pool.Add(e);
            }
            if (pool.Count == 0) return null;
            int idx = UnityEngine.Random.Range(0, pool.Count);
            return pool[idx];
        }

        [ClientRpc]
        private void BroadcastEventClientRpc(string title, string description)
        {
            OnEventStartedClient?.Invoke(title, description);
        }

        [ClientRpc]
        private void BroadcastEventKeysClientRpc(int titleKey, int descriptionKey, string imageKey, string audioKey)
        {
            OnEventStartedClientKeys?.Invoke(titleKey, descriptionKey, imageKey, audioKey);
        }

        [ClientRpc]
        private void NotifyEventEndedClientRpc()
        {
            OnEventEndedClient?.Invoke();
        }

        // Optional auto-detection via reflection to avoid compile-time dependency on Zone
        private void PollRunForWave()
        {
            try
            {

                int wave = Run.instance.CPUcontroller.zone.currentWave;
                if (wave != lastObservedWave)
                {
                    // detect transitions
                    if (lastObservedWave >= 0)
                    {
                        // End previous wave
                        NotifyWaveEnded(lastObservedWave);
                    }
                    lastObservedWave = wave;
                    NotifyWaveStarted(wave);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SurvivorEventSystem] PollRunForWave error: {ex.Message}");
            }
        }

        // Lightweight mode check without strong dependency
        private bool IsCurrentModeSurvivor()
        {
            return Run.instance.CPUcontroller.zone.serverMode == GameMode.Survivor;
        }

        // Public entry so events can request a UI refresh after changing modifiers
        public static void RequestBroadcastMonsterBonus()
        {
            if (Instance != null && Instance.IsServer)
            {
                Instance.BroadcastCurrentMonsterBonus();
            }
        }

        // Compute and broadcast the current monster bonus to clients
        private void BroadcastCurrentMonsterBonus()
        {
            if (!IsServer) return;
            try
            {
                float total = CalculateCurrentMonsterTotalBonus();
                SetMonsterBonusClientRpc(total);
            }
            catch { }
        }

        [ClientRpc]
        private void SetMonsterBonusClientRpc(float totalBonus)
        {
            if (WaveInfoUI.instance != null)
            {
                WaveInfoUI.instance.ApplyServerTotalBonus(totalBonus);
            }
        }

        private float CalculateCurrentMonsterTotalBonus()
        {
            var z = Run.instance?.CPUcontroller?.zone;
            int currentWave = z != null ? z.currentWave : 1;
            return SurvivorModifiers.GetMonsterScalingFactor(currentWave);
        }

        // Apply player HP multiplier and extra boss spawns right after an event is applied
        private void ApplyPerWaveEffectsAfterEventApplied()
        {
            if (!IsServer) return;

            // Spawn extra bosses if requested by event
            if (SurvivorModifiers.extraBossCountThisWave > 0)
            {
                try
                {
                    Run.instance.CPUcontroller.zone.monsterNumber += SurvivorModifiers.extraBossCountThisWave;
                    for (int i = 0; i < SurvivorModifiers.extraBossCountThisWave; i++)
                    {
                        Run.instance.CPUcontroller.zone.SpawnMonster(0, 3, true); // random boss
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SurvivorEventSystem] Failed to spawn extra bosses: " + e.Message);
                }
            }

            // Adjust alive players' max HP according to multiplier (non-monsters only)
            try
            {
                List<PlayerReference> targets = new List<PlayerReference>();

                foreach (var p in targets)
                {
                    if (p == null || p.playerStatistics == null) continue;
                    if (p.playerClasses != null && p.playerClasses.isMonster) continue;

                    var stats = p.playerStatistics.playerStatData;
                    string playerName = p.gameObject != null ? p.gameObject.name : "<unknown>";
                    float oldMax = stats.maxHealth;
                    float oldHealth = stats.health;
                    // Capture baseline for server if not set yet
                    if (p.playerStatistics.baseMaxHealth <= 0f)
                    {
                        p.playerStatistics.baseMaxHealth = oldMax;
                    }
                    float baseMax = p.playerStatistics.baseMaxHealth > 0f ? p.playerStatistics.baseMaxHealth : stats.maxHealth;
                    float newMax = Mathf.Max(1f, baseMax * SurvivorModifiers.playerMaxHealthMultiplier);
                    float oldMaxNonZero = Mathf.Max(1f, oldMax);
                    float scaledHealth = stats.health * (newMax / oldMaxNonZero); // preserve percentage
                    stats.maxHealth = newMax;
                    stats.health = Mathf.Clamp(scaledHealth, 0f, newMax);
                    p.playerStatistics.playerStatData = stats;
                    Debug.Log($"[SurvivorEventSystem] HP update for {playerName}: Max {oldMax} -> {stats.maxHealth}, Health {oldHealth} -> {stats.health} (multiplier={SurvivorModifiers.playerMaxHealthMultiplier})");
                    // ClientRpc synced on the player's NetworkBehaviour ensures the local player's UI updates
                    p.playerStatistics.UpdateSoulHealthClientRpc(stats);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurvivorEventSystem] Failed to apply HP multiplier: " + e.Message);
            }

            // Spawn Stone Heart at a random player spawn
            if (SurvivorModifiers.defendStoneHeartThisWave && spawnedStoneHeart == null)
            {
                TrySpawnStoneHeart();
            }
            // Force transformations according to event type (block human -> force spirit, block spirit -> force human)
            try
            {
                ApplyForcedTransformationForEvent();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurvivorEventSystem] Failed to apply forced transformations: " + e.Message);
            }
        }

        // If event blocks human transforms, force players into spirit at start; if it blocks spirit, force them into human.
        private void ApplyForcedTransformationForEvent()
        {
            if (!IsServer || activeEvent == null) return;

            bool forceSpirit = activeEvent is SurvivorMode.Events.BlockHumanTransformEvent;
            bool forceHuman = activeEvent is SurvivorMode.Events.BlockSpiritTransformEvent;
            if (!forceSpirit && !forceHuman) return;

            List<PlayerReference> targets = new List<PlayerReference>();

            foreach (var p in targets)
            {
                if (p == null || p.PlayerReincarnation == null || p.playerStatistics == null) continue;
                if (p.playerClasses != null && p.playerClasses.isMonster) continue;
                if (p.playerStatistics.playerStatData.health <= 0f) continue;

                if (forceSpirit && !p.PlayerReincarnation.IsReincarnation)
                {
                    Debug.Log($"[SurvivorEventSystem] Forcing spirit transform on {p.gameObject.name}");
                    p.PlayerReincarnation.Transformation(p.playerStatistics.playerDataGame, p);
                }
                else if (forceHuman && p.PlayerReincarnation.IsReincarnation)
                {
                    Debug.Log($"[SurvivorEventSystem] Forcing human form on {p.gameObject.name}");
                    p.PlayerReincarnation.DeTransformation(p.playerStatistics.playerDataGame, p);
                }
            }
        }

        // Spawns a Stone Heart at a random player spawn point (server only)
        private void TrySpawnStoneHeart()
        {
            if (!IsServer) return;
            try
            {
                var zone = Run.instance?.CPUcontroller?.zone;
                if (zone == null)
                {
                    Debug.LogWarning("[SurvivorEventSystem] No Zone available to spawn Stone Heart.");
                    return;
                }
                var spawns = zone.playerSpawnPoints;
                if (spawns == null || spawns.Count == 0)
                {
                    Debug.LogWarning("[SurvivorEventSystem] No player spawn points to place Stone Heart.");
                    return;
                }

                int start = UnityEngine.Random.Range(0, spawns.Count);
                int index = -1;
                for (int i = 0; i < spawns.Count; i++)
                {
                    int idx = (start + i) % spawns.Count;
                    if (!spawns[idx].isOccupied)
                    {
                        index = idx;
                        break;
                    }
                }
                if (index < 0) index = start; // fallback
                Vector3 pos = spawns[index].position;

                // Load prefab and spawn on network
                var prefab = Resources.Load<GameObject>("Prefabs/StoneHeart");
                if (prefab == null)
                {
                    Debug.LogError("[SurvivorEventSystem] Resources/Prefabs/StoneHeart prefab not found. Create it via Tools/Survivor/Create Stone Heart Prefab.");
                    return;
                }
                var heartGO = Instantiate(prefab, pos, Quaternion.identity);

                var no = heartGO.GetComponent<Unity.Netcode.NetworkObject>();
                no.Spawn();
                spawnedStoneHeart = no;
                spawnedHeartSpawnPointIndex = index;
                spawns[index].isOccupied = true;
                SetSpawnedObjectPositionForClients(spawnedStoneHeart, pos);
                Debug.Log("[SurvivorEventSystem] Stone Heart spawned at player spawn point #" + index);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurvivorEventSystem] Failed to spawn Stone Heart: " + e.Message);
            }
        }
        [ClientRpc]
        private void SetObjectPositionClientRpc(NetworkObjectReference objRef, Vector3 position)
        {
            if (objRef.TryGet(out NetworkObject no))
            {
                no.transform.position = position;
                OnStoneHeartAnnouncedClient?.Invoke(no);
            }
        }

        public void HandleStoneHeartDestroyed(StoneHeartTarget heart)
        {
            if (!IsServer) return;
            if (heart == null) return;

            var heartNetworkObject = heart.GetComponent<NetworkObject>();
            if (spawnedStoneHeart != null)
            {
                if (heartNetworkObject == null || heartNetworkObject.NetworkObjectId != spawnedStoneHeart.NetworkObjectId)
                {
                    return;
                }
            }

            var zone = Run.instance?.CPUcontroller?.zone;
            if (zone != null && spawnedHeartSpawnPointIndex >= 0 && spawnedHeartSpawnPointIndex < zone.playerSpawnPoints.Count)
            {
                zone.playerSpawnPoints[spawnedHeartSpawnPointIndex].isOccupied = false;
            }

            spawnedStoneHeart = null;
            spawnedHeartSpawnPointIndex = -1;
        }

        private void SetSpawnedObjectPositionForClients(NetworkObject obj, Vector3 position)
        {
            if (!IsServer || obj == null) return;
            SetObjectPositionClientRpc(obj, position);
        }
    }
}


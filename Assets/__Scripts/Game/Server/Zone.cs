using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using Json;
using Unity.Collections;
using SurvivorMode;

public enum ZonePhase : byte
{
    Stopped = 0,   // La zone est à l'arrêt; on affiche "repart dans ..."
    Shrinking = 1  // La zone se déplace; on affiche "s'arrête dans ..."
}


public class Zone : NetworkBehaviour
{
    public static event System.Action<int> OnWaveStartedClient;
    public static event System.Action<int> OnWaveEndedClient;

    public GameMode serverMode;
    [Header("Permet de d�finir un point de spawn pr�cis")]
    public Vector3 spawnOnSpecificPoint;
    [Header("Permet de cacher les joueurs entre eux")]
    public bool hideOtherPlayers;

    public GameObject[] smallMonsters;
    public GameObject[] advancedMonsters;
    public GameObject[] bossMonsters;
    public GameObject[] rareMonsters;
    private int zoneStep = 0;

    public List<GameObject> zoneSpawnObject;
    public List<Vector3> mushySpawnCoord;
    public List<Vector3> flochonSpawnCoord;
    public List<Vector3> ratoSpawnCoord;
    public List<Vector3> golemSpawnCoord;

    bool zoneSpawned = false;

    GameObject player;

    public float minDistanceBetweenSpawns = 16f; // Distance minimale entre les points de spawn

    public int monsterNumber;
    public float waveEndTime;

    public bool isGameEnded;

    [Header("Le monstre à spawn est ici:")]
    public int monsterNumberToSpawn = 5;
    private bool isSpawningMonsters = false;

    public int currentWave = 1;
    private int baseMonsterCount = 5; // Nombre de monstres de départ

    private int monsterLimit = 150;
    [HideInInspector] public bool isWaveActive = false;

    [HideInInspector] public bool battleStarted = false;

    // Séparation claire ici:
    public List<SpawnPoint> monsterSpawnPoints = new List<SpawnPoint>();
    public List<SpawnPoint> playerSpawnPoints = new List<SpawnPoint>();
    public bool isGeneratingMonsterPoints = false;
    public bool isGeneratingPlayerPoints = false;

    private Coroutine survivorWaveCoroutine;

    private Coroutine waveTimeCoroutine;

    private bool hasAnnouncedFirstWaveStart;

    // WAVE STREAMER:
    // private List<ConnectDatabase.SpiritData> allSpirits = new List<ConnectDatabase.SpiritData>();
    private int spiritStartIndex = 0;

    public int streamerId;

    private void OnEnable()
    {
#if UNITY_SERVER
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_SERVER
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
#endif
    }

    private void HandleClientConnected(ulong clientId)
    {
#if UNITY_SERVER
        if (!IsServer) return;
        if (serverMode == GameMode.Survivor)
        {
            BroadcastZoneTimer(clientId);
        }
#endif
    }

    [Header("Zone Reduction")]
    public string[] zoneAudioClipsKey;
    private Vector3 initialZoneScale;

    [Header("Survivor Settings")]
    [SerializeField] private float survivorTeleportRadius = 3f;
    [SerializeField] private float survivorMonsterSpawnOffset = 5f;
    [SerializeField] private float survivorNavMeshSampleRadius = 12f;
    [SerializeField] private float survivorRadiusMonster = 0f; // 0 = auto (use zone bounds)
    [SerializeField] private bool logSurvivorZone = true;
    private int lastSurvivorZoneIndex = -1;

    [SerializeField] private readonly float[] zoneShrinkFactors = { 1.0f, 0.75f, 0.50f, 0.25f, 0.15f, 0f };
    private const float shrinkDuration = 30f; // durée de l’animation
    private const float shrinkInterval = 150f; // 3 minutes

    private bool zoneStartMoving = false; // Utile pour l'audio qui déclenche game start

    [SerializeField] private GameObject zoneWarning;

    // Battle Royale: target center picked after first countdown
    private bool brCenterLocked = false;
    private Vector3 brCenter;


    private ZonePhase _currentPhase = ZonePhase.Stopped;
    private double _phaseEndServerTime = 0; // horodatage serveur (seconds)

    public ZonePhase GetCurrentPhase() => _currentPhase;
    public double GetPhaseEndTime() => _phaseEndServerTime;

    [Header("Minimap Indicator (Client-only scaling)")]
    [SerializeField] private Transform minimapIndicator;         // L'objet minimap à scaler
    [SerializeField] private float minimapScaleMultiplier = 1f;   // Ajuste le mapping si nécessaire
    [SerializeField] private Transform futureMinimapIndicator;    // Indicateur de la prochaine zone

    // Caches côté client pour un mapping absolu (pas cumulatif)
    private Vector3 _initialZoneScaleClient;
    private Vector3 _initialMinimapScale;
    private Vector3 _initialFutureMinimapScale;
    private bool _scalesCached;

    // Suivi serveur de la prochaine zone (pour les late joiners)
    private bool _hasFutureZonePreview;
    private Vector2 _futureZoneScaleXZ;
    private Vector3 _futureZoneCenter;



#if UNITY_SERVER
    private bool waitTimeExpired = false; // Déclaré en variable d'instance

    public void StartSurvivorMode()
    {
        Debug.Log("[Zone] Starting Survivor wave");
        hasAnnouncedFirstWaveStart = false;
        MoveSurvivorZoneToRandomMonsterPoint(teleportPlayers: true, avoidCurrent: true);
        survivorWaveCoroutine = StartCoroutine(SurvivorWaveRoutine());
    }

    public void MoveSurvivorZoneToRandomMonsterPoint(bool teleportPlayers, bool avoidCurrent)
    {
        if (!IsServer) return;
        EnsureInitialZoneScaleRecorded();
        Vector3 target = PickSurvivorZoneCenter(avoidCurrent);
        float searchHeight = Mathf.Max(survivorNavMeshSampleRadius, 200f);
        if (NavMesh.SamplePosition(target + Vector3.up * (searchHeight * 0.5f), out var hit, searchHeight, NavMesh.AllAreas))
        {
            target = hit.position;
        }
        ApplySurvivorZoneCenter(target, teleportPlayers);
    }

    private Vector3 PickSurvivorZoneCenter(bool avoidCurrent)
    {
        if (monsterSpawnPoints == null || monsterSpawnPoints.Count == 0)
            return transform.position;

        int attempts = Mathf.Max(1, monsterSpawnPoints.Count);
        Vector3 current2D = new Vector3(transform.position.x, 0f, transform.position.z);
        for (int i = 0; i < attempts; i++)
        {
            int idx = Random.Range(0, monsterSpawnPoints.Count);
            var candidate = monsterSpawnPoints[idx].position;
            if (!avoidCurrent || Vector3.Distance(new Vector3(candidate.x, 0f, candidate.z), current2D) > 1f)
            {
                lastSurvivorZoneIndex = idx;
                return candidate;
            }
        }

        // fallback: pick any (even if close)
        var pick = monsterSpawnPoints[Random.Range(0, monsterSpawnPoints.Count)];
        return pick.position;
    }

    private void EnsureInitialZoneScaleRecorded()
    {
        if (initialZoneScale == Vector3.zero)
        {
            initialZoneScale = transform.localScale;
        }
    }

    private Vector3 GetSurvivorTargetScale()
    {
        // Fixed survivor zone scale
        return new Vector3(5000f, 5000f, 50000f);
    }

    private void ApplySurvivorZoneCenter(Vector3 newCenter, bool teleportPlayers)
    {
        if (!IsServer) return;
        Vector3 dest = new Vector3(newCenter.x, transform.position.y, newCenter.z);

        // Adjust zone size to the requested survivor scale and sync to clients.
        Vector3 targetScale = GetSurvivorTargetScale();
        transform.localScale = targetScale;
        SyncZoneScaleClientRpc(new Vector2(targetScale.x, targetScale.y));

        transform.position = dest;
        SetZoneCenterClientRpc(dest);
        if (teleportPlayers)
        {
            TeleportPlayersInsideZone(dest);
        }
        if (logSurvivorZone)
        {
            Debug.Log($"[Zone] Survivor zone moved to {dest} (teleport={teleportPlayers}).");
        }
        // Force a full sync of zone state (position/scale/timer) to all clients.
        BroadcastZoneTimer();
    }

    private void TeleportPlayersInsideZone(Vector3 center)
    {
        // Spread players around a small radius near the center, sampling navmesh if possible.
        List<PlayerReference> targets = new List<PlayerReference>();

        int count = targets.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            var pr = targets[i];
            if (pr == null || pr.networkObject == null) continue;
            Vector3 offset = Quaternion.Euler(0f, angleStep * i, 0f) * (Vector3.forward * Mathf.Max(1f, survivorTeleportRadius));
            Vector3 dest = center + offset;
            float searchHeight = Mathf.Max(survivorNavMeshSampleRadius, 200f);
            if (NavMesh.SamplePosition(dest + Vector3.up * (searchHeight * 0.5f), out var hit, searchHeight, NavMesh.AllAreas))
            {
                dest = hit.position;
            }
            else if (monsterSpawnPoints != null && monsterSpawnPoints.Count > 0)
            {
                // Fallback to nearest monster spawn point (known navmesh)
                var nearest = monsterSpawnPoints.OrderBy(p => Vector3.SqrMagnitude(p.position - center)).First();
                dest = nearest.position;
            }
            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { pr.networkObject.OwnerClientId } }
            };
            TeleportLocalPlayerClientRpc(dest, rpcParams);
            PlayTeleportEffectClientRpc(new NetworkObjectReference(pr.networkObject));
            if (pr.playerStatistics != null)
            {
                pr.playerStatistics.isInsideZone = true;
            }
        }
    }

    public void StopSurvivorWaveRoutine()
    {
        if (survivorWaveCoroutine != null)
        {
            StopCoroutine(survivorWaveCoroutine);
            Debug.Log("Survivor wave routine stopped due to no players alive.");
        }
    }

    private IEnumerator SurvivorWaveRoutine()
    {
        yield break;
    }

    private IEnumerator WaitForMaxTime(float time)
    {
        yield return new WaitForSeconds(time);
        waitTimeExpired = true;
    }

    [ClientRpc]
    public void SetMonsterNameClientRpc(NetworkObjectReference monsterRef, FixedString64Bytes twitchName)
    {
        if (monsterRef.TryGet(out NetworkObject monsterNetworkObject))
        {
            GameObject monsterObject = monsterNetworkObject.gameObject;
            Billboard billboardEntity = monsterObject.GetComponentInChildren<Billboard>(true);
            monsterObject.name = twitchName.Value;
            if (billboardEntity != null)
            {
                billboardEntity.gameObject.SetActive(true);
                billboardEntity.entityName.text = twitchName.Value;
            }

            else
            {
                Debug.LogWarning("Le NetworkObjectReference du monstre n'a pas pu être résolu.");
            }
        }
    }
#endif

    #region TIMER ZONE

    // ==== API publique à appeler DANS TA LOGIQUE EXISTANTE ====
    // Appelle ceci quand la zone commence à BOUGER (rétrécissement).
    public void StartShrinkingPhase(float durationSeconds)
    {
        if (!IsServer) return;
        _currentPhase = ZonePhase.Shrinking;
        _phaseEndServerTime = NetworkManager.ServerTime.Time + durationSeconds;
        BroadcastZoneTimer();
    }

    // Appelle ceci quand la zone S’ARRÊTE (pause/immobile).
    public void StartStoppedPhase(float durationSeconds)
    {
        if (!IsServer) return;
        _currentPhase = ZonePhase.Stopped;
        _phaseEndServerTime = NetworkManager.ServerTime.Time + durationSeconds;
        BroadcastZoneTimer();
    }

    // Envoie à tous (ou à un client précis) l’état & l’heure de fin
    private void BroadcastZoneTimer(ulong? targetClientId = null)
    {
        var endTime = _phaseEndServerTime;
        var phase = _currentPhase;

        if (targetClientId.HasValue)
        {
            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId.Value } }
            };
            SetZoneTimerClientRpc(phase, endTime, rpcParams);
            // Also sync spatial state for late joiners
            Vector3 scale = transform.localScale;
            SyncZoneScaleClientRpc(new Vector2(scale.x, scale.y), rpcParams);
            SetZoneCenterClientRpc(transform.position, rpcParams);
            UpdateFutureZoneIndicatorClientRpc(_futureZoneScaleXZ, _futureZoneCenter, _hasFutureZonePreview, rpcParams);

        }
        else
        {
            SetZoneTimerClientRpc(phase, endTime);
        }
    }


    // Pour les late-joiners: le client demande l’état courant, le serveur renvoie juste à lui
    [ServerRpc(RequireOwnership = false)]
    public void RequestZoneTimerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        ulong sender = serverRpcParams.Receive.SenderClientId;
        BroadcastZoneTimer(sender);
    }

    // ==== RPC vers les clients ====
    [ClientRpc]
    private void SetZoneTimerClientRpc(ZonePhase phase, double endServerTime, ClientRpcParams clientRpcParams = default)
    {
        this._currentPhase = phase;
        this._phaseEndServerTime = endServerTime;
    }


    #endregion

    [ClientRpc]
    private void SendBonusSelectionClientRpc(int[] bonusIds)
    {
        // BonusSelectionUI.instance.ShowBonusSelection(bonusIds);
        OnWaveEndedClient?.Invoke(currentWave);
    }

    private IEnumerator WaitForMaxTime(float time, Action onTimeExpired)
    {
        yield return new WaitForSeconds(time);
        onTimeExpired?.Invoke();
    }

    public void AddSpawnPoint(Vector3 position, bool isPlayer)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            if (isPlayer)
                playerSpawnPoints.Add(new SpawnPoint(hit.position));
            else
                monsterSpawnPoints.Add(new SpawnPoint(hit.position));
        }
        else
        {
            Debug.LogWarning("La position sélectionnée n'est pas sur le NavMesh.");
        }
    }



    private void Start()
    {
        hasAnnouncedFirstWaveStart = false;
        if (SoundManager.Instance != null) SoundManager.Instance.Play2D("boom");
        // #if !UNITY_SERVER
        //         Destroy(GetComponent<CapsuleCollider>());
        // #endif

    }

    [ClientRpc]
    private void UpdateWaveInfoClientRpc(int newWave, int newMonsterNumber, float waveEndTime)
    {
        int oldWave = this.currentWave;
        this.currentWave = newWave;
        this.monsterNumber = newMonsterNumber;
        this.waveEndTime = waveEndTime;
        Debug.Log("Wave " + this.currentWave + ": " + this.monsterNumber);

        bool shouldNotifyStart = newWave > oldWave || (!hasAnnouncedFirstWaveStart && newWave == 1);
        if (shouldNotifyStart)
        {
            hasAnnouncedFirstWaveStart = true;
            OnWaveStartedClient?.Invoke(newWave);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Dessiner les points de spawn des monstres (vert)
        Gizmos.color = Color.green;
        foreach (var spawnPoint in monsterSpawnPoints)
            Handles.DrawWireDisc(spawnPoint.position, Vector3.up, 1f);

        // Dessiner les points de spawn des joueurs (bleu)
        Gizmos.color = Color.blue;
        foreach (var spawnPoint in playerSpawnPoints)
            Handles.DrawWireDisc(spawnPoint.position, Vector3.up, 1f);
    }

    public void RemoveSpawnPoint(Vector3 position)
    {
        var pointsList = monsterSpawnPoints;
        var pointsList2 = playerSpawnPoints;
        var pointToRemove = pointsList.FirstOrDefault(sp => Vector3.Distance(sp.position, position) < 3f);
        if (pointToRemove != null)
        {
            pointsList.Remove(pointToRemove);
            Debug.Log($"Point de spawn supprimé à la position : {position}");
        }
        var pointToRemove2 = pointsList2.FirstOrDefault(sp => Vector3.Distance(sp.position, position) < 3f);
        if (pointToRemove2 != null)
        {
            pointsList.Remove(pointToRemove2);
            Debug.Log($"Point de spawn supprimé à la position : {position}");
        }
    }

    // Fonction pour récupérer un spawn pour joueur
    public Vector3 GetPlayerSpawnPoint()
    {
        if (playerSpawnPoints.Count == 0)
        {
            Debug.LogError("Aucun point de spawn joueur enregistré !");
            return Vector3.zero;
        }

        var selectedSpawnPoint = playerSpawnPoints[Random.Range(0, playerSpawnPoints.Count)];
        return selectedSpawnPoint.position;
    }
#endif

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 || other.gameObject.layer == 9)
        {
            PlayerReference playerRef = other.GetComponent<PlayerReference>();
            playerRef.playerStatistics.isInsideZone = true;
            if (playerRef.networkObject.IsLocalPlayer && zoneWarning)
            {
                zoneWarning.SetActive(false);
            }

        }
    }

    public void OnTriggerExit(Collider other)
    {
#if UNITY_SERVER
        if (other.gameObject.layer == 5)
        {
            Destroy(other.gameObject);
        }
#endif
        if (other.gameObject.layer == 9 || other.gameObject.layer == 3)
        {
            PlayerReference playerRef = other.GetComponent<PlayerReference>();
            if (playerRef == null || playerRef.playerStatistics == null) return;
            playerRef.playerStatistics.isInsideZone = false;

            if (IsServer && serverMode == GameMode.Survivor && SurvivorModifiers.dangerZoneActiveThisWave)
            {
                if (playerRef.playerClasses != null && !playerRef.playerClasses.isMonster)
                {
                    SurvivorEventsApi.Instance?.ReportChallengeFailed();
                }
            }

            if (playerRef.networkObject.IsLocalPlayer && zoneWarning)
            {
                zoneWarning.SetActive(true);
                PlayZoneAudio(2);
            }

        }
    }


    // Spawn initial pack of BR monsters when countdown ends
    public void SpawnInitialBattleRoyaleMonsters()
    {
#if UNITY_SERVER
        if (!IsServer) return;
        if (serverMode != GameMode.BattleRoyale) return;

        int toSpawn = Mathf.Max(0, monsterNumberToSpawn - monsterNumber);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnMonster(0, 1, true);
            monsterNumber++;
        }
#endif
    }

    private IEnumerator ScaleTransformOverTime(Transform targetTransform, Vector3 targetScale, float duration)
    {
        if (targetTransform == null) yield break;
        Vector3 start = targetTransform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            targetTransform.localScale = Vector3.Lerp(start, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetTransform.localScale = targetScale;
    }

    private IEnumerator MoveTransformOverTime(Transform targetTransform, Vector3 targetPosition, float duration)
    {
        if (targetTransform == null) yield break;
        Vector3 start = targetTransform.position;
        Vector3 dest = new Vector3(targetPosition.x, start.y, targetPosition.z);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 next = Vector3.Lerp(start, dest, elapsed / duration);
            next.y = start.y;
            targetTransform.position = next;
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetTransform.position = dest;
    }

    private IEnumerator WaitForZoneToReach(Vector3 targetScale, Vector3 targetPosition, bool checkPosition, float tolerance = 0.01f)
    {
        // Waits until the zone transform matches expected scale/position (within tolerance)
        while (Vector3.Distance(transform.localScale, targetScale) > tolerance ||
              (checkPosition && Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) > tolerance))
        {
            yield return null;
        }
        // Snap to ensure we finish exactly on target
        transform.localScale = targetScale;
        if (checkPosition)
        {
            var p = transform.position;
            transform.position = new Vector3(targetPosition.x, p.y, targetPosition.z);
        }
    }

#if UNITY_SERVER
    void Update()
    {
        if (serverMode == GameMode.BattleRoyale && battleStarted)
        {
            if (monsterNumber < monsterNumberToSpawn && !isSpawningMonsters)
            {
                isSpawningMonsters = true;
                monsterNumber++;
                StartCoroutine(SpawnMonsterOverTime(0.5f));
            }
        }

    }

    IEnumerator SpawnMonsterOverTime(float spawnInterval)
    {
        Debug.Log(monsterNumber + ": Spawn du monstre.");
        SpawnMonster(0, 1, true);
        yield return new WaitForSeconds(spawnInterval);
        isSpawningMonsters = false;
    }

#else

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Cache une seule fois les scales de départ côté client
            if (!_scalesCached)
            {
                _initialZoneScaleClient = transform.localScale;
                if (minimapIndicator != null)
                    _initialMinimapScale = minimapIndicator.localScale;
                if (futureMinimapIndicator != null)
                    _initialFutureMinimapScale = futureMinimapIndicator.localScale;
                _scalesCached = true;
            }

            RequestZoneTimerServerRpc();
        }
    }

#endif

    #region ZONE


    private IEnumerator ReduceZone()
    {
        Debug.Log("Reducing zone...");
        initialZoneScale = transform.localScale;

        // Durée d'animation
        float duration = 30f;

        // Battle Royale: pick a center once at the start, then keep it for the rest of the match
        if (serverMode == GameMode.BattleRoyale && !brCenterLocked)
        {
            if (playerSpawnPoints != null && playerSpawnPoints.Count > 0)
            {
                var pick = playerSpawnPoints[Random.Range(0, playerSpawnPoints.Count)];
                brCenter = pick.position;
            }
            else
            {
                brCenter = transform.position; // fallback if no player points registered
            }
            brCenterLocked = true;
        }

        if (zoneShrinkFactors != null && zoneShrinkFactors.Length > 0)
        {
            Vector3 previewCenter = brCenterLocked ? brCenter : transform.position;
            Vector3 futureTarget = new Vector3(
                initialZoneScale.x * zoneShrinkFactors[0],
                initialZoneScale.y * zoneShrinkFactors[0],
                transform.localScale.z
            );
            PushFutureZonePreviewToClients(new Vector2(futureTarget.x, futureTarget.y), previewCenter, true);
        }
        else
        {
            PushFutureZonePreviewToClients(Vector2.zero, Vector3.zero, false);
        }

        for (int i = 0; i < zoneShrinkFactors.Length; i++)
        {
            Vector3 target = new Vector3(
                initialZoneScale.x * zoneShrinkFactors[i],
                initialZoneScale.y * zoneShrinkFactors[i],
                transform.localScale.z
            );

            Debug.Log($"Réduction zone vers {zoneShrinkFactors[i] * 100}% (Step {i})");

            if (IsServer) StartShrinkingPhase(shrinkDuration);

            // Animation client + serveur (scale + optional center move on first step in BR)
            bool moveCenterNow = (serverMode == GameMode.BattleRoyale && i == 0 && brCenterLocked);
            StartZoneReductionClientRpc(new Vector2(target.x, target.y), moveCenterNow, brCenter);
            yield return StartCoroutine(ScaleOverTime(target, duration));
            if (moveCenterNow)
            {
                yield return StartCoroutine(MoveTransformOverTime(transform, brCenter, duration));

                // pas besoin server side
                // yield return StartCoroutine(MoveTransformOverTime(minimapIndicator.transform, brCenter, duration));
            }

            yield return StartCoroutine(WaitForZoneToReach(target, brCenter, moveCenterNow));

            bool hasMoreSteps = i < zoneShrinkFactors.Length - 1;
            if (hasMoreSteps)
            {
                Vector3 nextTarget = new Vector3(
                    initialZoneScale.x * zoneShrinkFactors[i + 1],
                    initialZoneScale.y * zoneShrinkFactors[i + 1],
                    transform.localScale.z
                );
                Vector3 nextCenter = brCenterLocked ? brCenter : transform.position;
                PushFutureZonePreviewToClients(new Vector2(nextTarget.x, nextTarget.y), nextCenter, true);
            }
            else
            {
                PushFutureZonePreviewToClients(Vector2.zero, Vector3.zero, false);
            }

            float waitTime = shrinkInterval;

            if (hasMoreSteps && waitTime > 0f && IsServer)
            {
                StartStoppedPhase(waitTime);
                yield return new WaitForSeconds(waitTime);
            }
            else if (hasMoreSteps && waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    [ClientRpc]
    private void StartZoneReductionClientRpc(Vector2 targetXY, bool moveToCenter, Vector3 targetCenter, ClientRpcParams rpcParams = default)
    {
        Vector3 currentZone = transform.localScale;
        Vector3 targetZone = new Vector3(targetXY.x, targetXY.y, currentZone.z);

        // Cache si pas encore fait
        if (!_scalesCached)
        {
            _initialZoneScaleClient = currentZone;
            if (minimapIndicator != null) _initialMinimapScale = minimapIndicator.localScale;
            if (futureMinimapIndicator != null) _initialFutureMinimapScale = futureMinimapIndicator.localScale;
            _scalesCached = true;
        }

        // Ratio absolu par rapport à l’échelle initiale côté client
        float shrinkRatio = (_initialZoneScaleClient.x != 0f) ? (targetZone.x / _initialZoneScaleClient.x) : 1f;

        // Cible minimap = échelle initiale minimap * ratio * multiplicateur éventuel
        if (minimapIndicator != null)
        {
            Vector3 minimapTarget = _initialMinimapScale * (shrinkRatio * minimapScaleMultiplier);
            StartCoroutine(ScaleTransformOverTime(minimapIndicator, minimapTarget, shrinkDuration));
        }

        // Zone (comportement existant)
        transform.localScale = currentZone; // force sync point de départ
        StartCoroutine(ScaleOverTime(targetZone, shrinkDuration));

        // Optional move of the zone center (only on first reduction in BR)
        if (moveToCenter)
        {
            StartCoroutine(MoveTransformOverTime(transform, targetCenter, shrinkDuration));
            StartCoroutine(MoveTransformOverTime(minimapIndicator.transform, targetCenter, shrinkDuration));

        }

        PlayZoneAudio(!zoneStartMoving ? 0 : 1);
        if (!zoneStartMoving)
        {
            SoundManager.Instance.StopByKey("matchmaking-music");
            SoundManager.Instance.PlayLoop2D("dechys");
            zoneStartMoving = true;
        }
    }

    [ClientRpc]
    private void UpdateFutureZoneIndicatorClientRpc(Vector2 targetXY, Vector3 targetCenter, bool hasFuture, ClientRpcParams rpcParams = default)
    {
        ApplyFutureMinimapIndicator(hasFuture, targetXY, targetCenter);
    }

    private void ApplyFutureMinimapIndicator(bool hasFuture, Vector2 targetXY, Vector3 targetCenter)
    {
        if (futureMinimapIndicator == null) return;

        if (!_scalesCached)
        {
            _initialZoneScaleClient = transform.localScale;
            if (minimapIndicator != null) _initialMinimapScale = minimapIndicator.localScale;
            _initialFutureMinimapScale = futureMinimapIndicator.localScale;
            _scalesCached = true;
        }

        futureMinimapIndicator.gameObject.SetActive(hasFuture);
        if (!hasFuture) return;

        float shrinkRatio = (_initialZoneScaleClient.x != 0f) ? (targetXY.x / _initialZoneScaleClient.x) : 1f;
        Vector3 baseScale = _initialFutureMinimapScale != Vector3.zero ? _initialFutureMinimapScale : futureMinimapIndicator.localScale;
        Vector3 minimapTarget = baseScale * (shrinkRatio * minimapScaleMultiplier);

        futureMinimapIndicator.localScale = minimapTarget;
        futureMinimapIndicator.position = new Vector3(targetCenter.x, futureMinimapIndicator.position.y, targetCenter.z);
    }

    private void PushFutureZonePreviewToClients(Vector2 targetXY, Vector3 targetCenter, bool hasFuture, ClientRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        _hasFutureZonePreview = hasFuture;
        _futureZoneScaleXZ = targetXY;
        _futureZoneCenter = targetCenter;
        UpdateFutureZoneIndicatorClientRpc(targetXY, targetCenter, hasFuture, rpcParams);
    }

    [ClientRpc]
    public void SyncZoneScaleClientRpc(Vector2 scaleXZ, ClientRpcParams rpcParams = default)
    {
        Vector3 targetZone = new Vector3(scaleXZ.x, scaleXZ.y, transform.localScale.z);
        transform.localScale = targetZone;

        if (!_scalesCached)
        {
            _initialZoneScaleClient = targetZone;
            if (minimapIndicator != null) _initialMinimapScale = minimapIndicator.localScale;
            if (futureMinimapIndicator != null) _initialFutureMinimapScale = futureMinimapIndicator.localScale;
            _scalesCached = true;
        }

        if (minimapIndicator != null && _initialZoneScaleClient.x != 0f)
        {
            float shrinkRatio = targetZone.x / _initialZoneScaleClient.x;
            Vector3 minimapTarget = _initialMinimapScale * (shrinkRatio * minimapScaleMultiplier);
            // Pose directe + relance anim pour suivre la zone si nécessaire
            minimapIndicator.localScale = minimapTarget;
            StartCoroutine(ScaleTransformOverTime(minimapIndicator, minimapTarget, shrinkDuration));
        }

        // Pour forcer l'animation zone si elle est censée continuer
        StartCoroutine(ScaleOverTime(targetZone, shrinkDuration));
    }

    [ClientRpc]
    private void SetZoneCenterClientRpc(Vector3 center, ClientRpcParams rpcParams = default)
    {
        Debug.Log("SetZoneCenterClientRpc");
        Vector3 p = transform.position;
        transform.position = new Vector3(center.x, p.y, center.z);
    }

    [ClientRpc]
    private void TeleportLocalPlayerClientRpc(Vector3 destination, ClientRpcParams rpcParams = default)
    {
        var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj == null) return;
        var pr = localObj.GetComponent<PlayerReference>();
        if (pr == null) return;
        try
        {
            if (pr.characterActor != null)
            {
                pr.characterActor.enabled = false;
                pr.rigidBody.position = destination;
                pr.characterActor.RigidbodyComponent.enabled = true;
                pr.characterActor.enabled = true;
                pr.characterActor.SweepAndTeleport(destination);
                return;
            }
            if (pr.agent != null && pr.agent.enabled)
            {
                pr.agent.Warp(destination);
                return;
            }
            pr.transform.position = destination;
        }
        catch
        {
            pr.transform.position = destination;
        }
    }

    [ClientRpc]
    private void PlayTeleportEffectClientRpc(NetworkObjectReference playerRef, ClientRpcParams rpcParams = default)
    {
        if (playerRef.TryGet(out var no))
        {
            var pr = no.GetComponent<PlayerReference>();
            if (pr != null && no.IsLocalPlayer)
            {
                var ui = UnityEngine.Object.FindObjectOfType<cooldownUI>();
                if (ui != null) ui.PlaySoulEffect();
            }
        }
    }

    private IEnumerator ScaleOverTime(Vector3 targetScale, float duration)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(start, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public void PlayZoneAudio(int index)
    {
        if (index < zoneAudioClipsKey.Length && zoneAudioClipsKey[index] != null)
        {
            SoundManager.Instance.Play2D(zoneAudioClipsKey[index]);
        }
    }

    #endregion

    public float GetZoneRadius()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            var extents = col.bounds.extents;
            return Mathf.Min(extents.x, extents.z);
        }
        var scale = transform.localScale;
        return Mathf.Min(scale.x, scale.z) * 0.5f;
    }

    public Vector3 GetAvailableSpawnPoint(bool isPlayer)
    {
        if (!isPlayer && serverMode == GameMode.Survivor)
        {
            var perimeter = GetSurvivorPerimeterSpawnPosition();
            if (perimeter != Vector3.zero)
                return perimeter;
        }

        return GetSpawnPointFromList(isPlayer);
    }

    private Vector3 GetSpawnPointFromList(bool isPlayer)
    {
        var availableSpawnPoints = isPlayer
            ? playerSpawnPoints.Where(elt => !elt.isOccupied).ToList()
            : monsterSpawnPoints.Where(elt => !elt.isOccupied).ToList();

        if (!isPlayer && availableSpawnPoints.Count < 3)
        {
            foreach (var spawnPoint in monsterSpawnPoints)
                spawnPoint.isOccupied = false;
            availableSpawnPoints = monsterSpawnPoints.ToList();
        }

        if (availableSpawnPoints.Count > 0)
        {
            var selectedSpawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
            selectedSpawnPoint.isOccupied = true;
            return selectedSpawnPoint.position;
        }

        Debug.Log("Attention: il faut enregistrer des positions de spawn pour les monstres");
        return Vector3.zero;
    }

    private Vector3 GetSurvivorPerimeterSpawnPosition()
    {
        Vector3 center = transform.position;
        float baseRadius = survivorRadiusMonster > 0f ? survivorRadiusMonster : GetZoneRadius();
        float spawnRadius = baseRadius + survivorMonsterSpawnOffset;
        const int maxAttempts = 12;
        Vector3? bestHit = null;
        float bestDistSq = float.MaxValue;
        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = center + dir * spawnRadius;
            float searchHeight = Mathf.Max(survivorNavMeshSampleRadius, 200f);
            if (NavMesh.SamplePosition(candidate + Vector3.up * (searchHeight * 0.5f), out var hit, searchHeight, NavMesh.AllAreas))
            {
                float distSq = Vector3.SqrMagnitude(new Vector3(hit.position.x, 0f, hit.position.z) - new Vector3(candidate.x, 0f, candidate.z));
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestHit = hit.position;
                    if (logSurvivorZone)
                    {
                        Debug.Log($"[Zone] Survivor monster spawn candidate @ {hit.position} (angleDeg={angle * Mathf.Rad2Deg:F1}, radius={spawnRadius:F1}, distFromEdge={Mathf.Sqrt(distSq):F2})");
                    }
                }
            }
        }

        if (bestHit.HasValue)
        {
            return bestHit.Value;
        }

        // Fallback: try center sampling
        float fallbackHeight = Mathf.Max(survivorNavMeshSampleRadius, 200f);
        if (NavMesh.SamplePosition(center + Vector3.up * (fallbackHeight * 0.5f), out var centerHit, fallbackHeight, NavMesh.AllAreas))
        {
            if (logSurvivorZone)
            {
                Debug.Log($"[Zone] Survivor monster spawn fallback -> center sampled {centerHit.position}");
            }
            return centerHit.position;
        }

        if (logSurvivorZone)
        {
            Debug.Log($"[Zone] Survivor monster spawn fallback -> center {center}");
        }
        return center;
    }

    IEnumerator SpawnSpecificZone()
    {
        yield return new WaitForSeconds(2.0f);
        GameObject monstre;
        for (int i = 0; i < mushySpawnCoord.Count; i++)
        {
            monstre = Instantiate(zoneSpawnObject[0], mushySpawnCoord[i], Quaternion.identity);
            monstre.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < flochonSpawnCoord.Count; i++)
        {
            monstre = Instantiate(zoneSpawnObject[1], flochonSpawnCoord[i], Quaternion.identity);
            monstre.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < ratoSpawnCoord.Count; i++)
        {
            monstre = Instantiate(zoneSpawnObject[2], ratoSpawnCoord[i], Quaternion.identity);
            monstre.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < golemSpawnCoord.Count; i++)
        {
            monstre = Instantiate(zoneSpawnObject[3], golemSpawnCoord[i], Quaternion.identity);
            monstre.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(0.1f);
        }
    }


    public GameObject SpawnMonster(int index, int type, bool isRandom)
    {

        /* smallMonsters, advancedMonsters, bossMonsters, rareMonsters */
        if (IsServer)
        {
            GameObject monstre = null;
            Vector3 spawnPosition = GetAvailableSpawnPoint(false);

            switch (type)
            {
                case 1: // smallMonsters
                    int randomIndex = Random.Range(0, smallMonsters.Length);
                    monstre = Instantiate(smallMonsters[isRandom ? randomIndex : index], spawnPosition, Quaternion.identity);
                    monstre.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    break;

                case 2: // advancedMonsters
                    Vector3 mPosition = spawnPosition;
                    monstre = Instantiate(advancedMonsters[isRandom ? Random.Range(0, advancedMonsters.Length - 1) : index], mPosition, Quaternion.identity);
                    monstre.GetComponent<NetworkObject>().Spawn();
                    monstre.transform.position = monstre.GetComponent<Follow>().defaultPosition = mPosition;
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    break;

                case 3: // bossMonsters
                    Vector3 position = spawnPosition;
                    Vector3 pos1 = new Vector3(position.x + 25, position.y, position.z + 25);
                    Vector3 pos2 = new Vector3(position.x + 25, position.y, position.z - 25);
                    Vector3 pos3 = new Vector3(position.x - 25, position.y, position.z + 25);
                    var boss = Instantiate(bossMonsters[isRandom ? Random.Range(0, bossMonsters.Length - 1) : index], position, Quaternion.identity);
                    boss.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    // var s1 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos1, Quaternion.identity);
                    // s1.GetComponent<NetworkObject>().Spawn();
                    // MonsterUpgradeApplier.ApplyOn(s1);
                    // var s2 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos2, Quaternion.identity);
                    // s2.GetComponent<NetworkObject>().Spawn();
                    // MonsterUpgradeApplier.ApplyOn(s2);
                    // var s3 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos3, Quaternion.identity);
                    // s3.GetComponent<NetworkObject>().Spawn();
                    // MonsterUpgradeApplier.ApplyOn(s3);
                    break;

                case 4: // rareMonsters
                    position = spawnPosition;
                    pos1 = new Vector3(position.x + 25, position.y, position.z + 25);
                    pos2 = new Vector3(position.x + 25, position.y, position.z - 25);
                    pos3 = new Vector3(position.x - 25, position.y, position.z + 25);
                    var rare = Instantiate(rareMonsters[isRandom ? Random.Range(0, rareMonsters.Length - 1) : index], position, Quaternion.identity);
                    rare.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier

                    var rs1 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos1, Quaternion.identity);
                    rs1.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    var rs2 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos2, Quaternion.identity);
                    rs2.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    var rs3 = Instantiate(smallMonsters[isRandom ? Random.Range(0, smallMonsters.Length - 1) : index], pos3, Quaternion.identity);
                    rs3.GetComponent<NetworkObject>().Spawn();
                    // Unified scaling handled in Follow + PlayerStatistics; skip legacy applier
                    break;
            }
            return monstre;
        }
        return null;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Zone))]
    public class ZoneEditor : Editor
    {
        public void OnSceneGUI()
        {
            Zone zone = (Zone)target;

            if (zone.isGeneratingMonsterPoints || zone.isGeneratingPlayerPoints)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));

            }

            Event e = Event.current;
            if (e.type == EventType.MouseDown && (zone.isGeneratingMonsterPoints || zone.isGeneratingPlayerPoints) && Event.current.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Undo.RecordObject(zone, "Add Spawn Point");
                    zone.AddSpawnPoint(hit.point, zone.isGeneratingPlayerPoints);
                    e.Use();
                }
            }


            if (e.type == EventType.MouseDown && (zone.isGeneratingMonsterPoints || zone.isGeneratingPlayerPoints) && Event.current.button == 1)
            { // Clic droit pour supprimer
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    zone.RemoveSpawnPoint(hit.point);
                    Event.current.Use(); // Empêche le clic de passer à d'autres objets
                }

            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Zone zone = (Zone)target;

            GUILayout.Space(10);
            GUILayout.Label("Générer Spawnpoints Joueurs (Bleu)");
            if (GUILayout.Button("Start Generating Player Spawn Points"))
                zone.isGeneratingPlayerPoints = true;
            if (GUILayout.Button("Stop Generating Player Spawn Points"))
                zone.isGeneratingPlayerPoints = false;

            GUILayout.Space(10);
            GUILayout.Label("Générer Spawnpoints Monstres (Vert)");
            if (GUILayout.Button("Start Generating Monster Spawn Points"))
                zone.isGeneratingMonsterPoints = true;
            if (GUILayout.Button("Stop Generating Monster Spawn Points"))
                zone.isGeneratingMonsterPoints = false;
        }
    }

#endif

    [Serializable]
    public class SpawnPoint
    {
        public Vector3 position;
        public bool isOccupied;

        public SpawnPoint(Vector3 position)
        {
            this.position = position;
            isOccupied = false;
        }




    }
}





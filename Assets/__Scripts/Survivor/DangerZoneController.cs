using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SurvivorMode
{
    public class DangerZoneController : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float waitAtPointSeconds = 10f;
        [SerializeField] private float teleportRadius = 3f;

        private Coroutine _moveRoutine;
        private readonly NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
            writePerm: NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                transform.position = netPosition.Value;
                netPosition.OnValueChanged += (_, newVal) => transform.position = newVal;
            }
        }

        public void Begin(Zone zone)
        {
            if (!IsServer || zone == null) return;
            // Choose a random starting player spawn point
            Vector3 start = transform.position;
            if (zone.playerSpawnPoints != null && zone.playerSpawnPoints.Count > 0)
            {
                int startIdx = Random.Range(0, zone.playerSpawnPoints.Count);
                start = zone.playerSpawnPoints[startIdx].position;
            }
            // Place the zone immediately and broadcast position
            transform.position = start;
            netPosition.Value = start;
            // Teleport players immediately using this known world position (no dependency on zone replication)
            TeleportAllPlayersToPosition(start);
            // Start moving randomly between points (skip the initial one)
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveBetweenSpawnPoints(zone, 1, randomize: true));
        }

        public void Stop()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }
        }

        private IEnumerator MoveBetweenSpawnPoints(Zone zone, int startIndex = 0, bool randomize = false)
        {
            if (zone.playerSpawnPoints == null || zone.playerSpawnPoints.Count == 0)
                yield break;

            int index = Mathf.Max(0, startIndex);
            var root = transform;
            while (true)
            {
                // Choose next target (random distinct index if requested)
                if (randomize)
                {
                    int next;
                    int count = zone.playerSpawnPoints.Count;
                    if (count > 1)
                    {
                        do { next = Random.Range(0, count); } while (next == index);
                        index = next;
                    }
                    else
                    {
                        index = 0;
                    }
                }
                var target = zone.playerSpawnPoints[index % zone.playerSpawnPoints.Count].position;
                while ((root.position - target).sqrMagnitude > 0.25f)
                {
                    root.position = Vector3.MoveTowards(root.position, target, moveSpeed * Time.deltaTime);
                    netPosition.Value = root.position;
                    yield return null;
                }
                yield return new WaitForSeconds(waitAtPointSeconds);
                if (!randomize) index++;
            }
        }

        private void TeleportAllPlayersToZone()
        {
            TeleportAllPlayersToPosition(transform.position);
        }

        private void TeleportAllPlayersToPosition(Vector3 center)
        {
            var players = GameObject.FindObjectsOfType<PlayerReference>(true);
            float angleStep = players.Length > 0 ? (360f / players.Length) : 360f;
            int i = 0;
            foreach (var pr in players)
            {
                if (pr == null) continue;
                Vector3 offset = Quaternion.Euler(0, angleStep * i, 0) * (Vector3.forward * teleportRadius);
                Vector3 dest = center + offset;
                // Teleport is executed client-side on the owning client to respect local movement authority
                var rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { pr.OwnerClientId } }
                };
                TeleportLocalPlayerClientRpc(dest, rpcParams);
                PlayTeleportEffectClientRpc(pr.NetworkObject);
                i++;
            }
        }

        [ClientRpc]
        private void PlayTeleportEffectClientRpc(NetworkObjectReference playerRef)
        {
            if (playerRef.TryGet(out var no))
            {
                var pr = no.GetComponent<PlayerReference>();
                if (pr != null && no.IsLocalPlayer)
                {
                    var ui = Object.FindObjectOfType<cooldownUI>();
                    if (ui != null) ui.PlaySoulEffect();
                }
            }
        }

        [ClientRpc]
        private void TeleportLocalPlayerClientRpc(Vector3 destination, ClientRpcParams clientRpcParams = default)
        {
            // Find local player and perform local-authoritative teleport
            var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
            if (localObj == null) return;
            var pr = localObj.GetComponent<PlayerReference>();
            if (pr == null) return;
            // Prefer CharacterActor sweep (smooth/physics-safe), then NavMeshAgent warp, then transform
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
    }
}

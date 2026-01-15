using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class PhysicsMonster : NetworkBehaviour
{
    private Follow follow;
    private CombatMonster combatMonster;
    private Rigidbody rigidBody;
    private CapsuleCollider capsule;

    private Coroutine pushCoroutine;
    [HideInInspector] public bool isInBlockMove = false;

    private float originalSpeed;

    // --- LES LIGNES QUE TU VOULAIS AJOUTER ---
    private struct PushEntry
    {
        public Vector3 velocity;
        public float endTime;
    }

    private readonly List<PushEntry> activePushes = new List<PushEntry>(4);
    private bool pushFirstWarpDone = false;
    private Vector3 cachedStartPos;
    private Quaternion cachedStartRot;

    private bool prevKinematic;
    public bool IsPushing => pushCoroutine != null;

    [SerializeField] private float waitBeforeNormalState = 0.3f;
    private float pushReleaseAt = 0f;
    public bool IsPushLocked => combatMonster.blockCast || IsPushing || Time.time < pushReleaseAt;
    // ------------------------------------------

    private void Awake()
    {
        follow = GetComponent<Follow>();
        combatMonster = GetComponent<CombatMonster>();
    }

    public void TriggerPush(Vector3 pushDirection, float pushDuration)
    {
        Vector3 velocity = pushDirection * 0.8f;

#if UNITY_SERVER
        TriggerPushClientRpc(velocity, pushDuration, follow.agent.transform.position, follow.agent.transform.rotation);
#endif
        EnqueuePush(velocity, pushDuration, follow.agent.transform.position, follow.agent.transform.rotation);
    }

    [ClientRpc]
    public void TriggerPushClientRpc(Vector3 velocity, float pushDuration, Vector3 startPosition, Quaternion startRotation)
    {
        EnqueuePush(velocity, pushDuration, startPosition, startRotation);
    }

    private void EnqueuePush(Vector3 velocity, float duration, Vector3 startPosition, Quaternion startRotation)
    {
        float endTime = Time.time + Mathf.Max(0.01f, duration);
        activePushes.Add(new PushEntry { velocity = velocity, endTime = endTime });

        pushReleaseAt = Mathf.Max(pushReleaseAt, endTime + waitBeforeNormalState);

        combatMonster.blockCast = true;
        Debug.Log("BLOCK CAST: TRUE");

        if (!IsPushing)
        {
            pushFirstWarpDone = false;
            cachedStartPos = startPosition;
            cachedStartRot = startRotation;
            pushCoroutine = StartCoroutine(PushController());
        }
    }

    public IEnumerator PushController()
    {
        if (!pushFirstWarpDone)
        {
            if (rigidBody == null) rigidBody = GetComponent<Rigidbody>();
            if (capsule == null) capsule = (follow.agent != null ? follow.agent.GetComponent<CapsuleCollider>() : null) ?? GetComponent<CapsuleCollider>();
            if (rigidBody != null)
            {
                prevKinematic = rigidBody.isKinematic;
                rigidBody.isKinematic = true;
            }

            follow.agent.transform.rotation = cachedStartRot;
            follow.agent.Warp(cachedStartPos);
            follow.agent.enabled = false;
            combatMonster.blockCast = true;
            pushFirstWarpDone = true;
        }

        while (true)
        {
            float now = Time.time;
            for (int i = activePushes.Count - 1; i >= 0; --i)
                if (activePushes[i].endTime <= now)
                    activePushes.RemoveAt(i);

            if (activePushes.Count == 0)
            {
                if (Time.time < pushReleaseAt)
                {
                    yield return null;
                    continue;
                }
                break;
            }

            Vector3 totalVelocity = Vector3.zero;
            for (int i = 0; i < activePushes.Count; i++)
                totalVelocity += activePushes[i].velocity;

            Vector3 currentPosition = follow.agent.transform.position;
            Vector3 nextPosition = currentPosition + totalVelocity * Time.deltaTime;

            if (totalVelocity.y < 0f)
            {
                Vector3 origin = currentPosition + Vector3.up * 0.1f;
                float distance = (nextPosition - currentPosition).magnitude + 0.1f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, LayerMask.GetMask("Ground")))
                {
                    for (int i = activePushes.Count - 1; i >= 0; --i)
                        if (activePushes[i].velocity.y < 0f)
                            activePushes.RemoveAt(i);
                    yield return null;
                    continue;
                }
            }

            int obstacleMask = LayerMask.GetMask("Default", "Ground");
            float skin = 0.02f;
            int maxSlides = 2;
            Vector3 delta = nextPosition - currentPosition;
            Vector3 newPos = currentPosition;
            if (delta.sqrMagnitude > 1e-8f)
            {
                GetCapsuleWorld(capsule, out Vector3 baseP1, out Vector3 baseP2, out float capRadius);
                Vector3 offset = newPos - follow.agent.transform.position;
                Vector3 p1 = baseP1 + offset;
                Vector3 p2 = baseP2 + offset;

                for (int i = 0; i < maxSlides && delta.sqrMagnitude > 1e-8f; i++)
                {
                    Vector3 dir = delta.normalized;
                    float dist = delta.magnitude;
                    if (Physics.CapsuleCast(p1, p2, capRadius, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        float moveDist = Mathf.Max(hit.distance - skin, 0f);
                        newPos += dir * moveDist;
                        delta -= dir * moveDist;
                        delta = Vector3.ProjectOnPlane(delta, hit.normal);
                        offset = newPos - follow.agent.transform.position;
                        p1 = baseP1 + offset;
                        p2 = baseP2 + offset;
                    }
                    else
                    {
                        newPos += dir * dist;
                        delta = Vector3.zero;
                    }
                }
                follow.agent.transform.position = newPos;
            }
            else
            {
                follow.agent.transform.position = nextPosition;
            }
            yield return null;
        }

        float safetyTimer = 0f;
        while (!IsGrounded() && safetyTimer < 3f)
        {
            follow.agent.transform.position += Vector3.down * 7f * Time.deltaTime;
            safetyTimer += Time.deltaTime;
            yield return null;
        }

        if (rigidBody != null) rigidBody.isKinematic = prevKinematic;

        if (!follow.agent.enabled) follow.agent.enabled = true;
        combatMonster.blockCast = false;

        if (!follow.agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                follow.agent.Warp(hit.position);
            else
                Debug.LogWarning("Pas de NavMesh trouvé pour re-warp.");
        }

        pushCoroutine = null;

        if (follow.cible == null) combatMonster.AskNearestTargetServerRpc();
    }

    private void GetCapsuleWorld(CapsuleCollider col, out Vector3 p1, out Vector3 p2, out float radius)
    {
        if (col == null)
        {
            Vector3 basePos = (follow.agent != null ? follow.agent.transform.position : transform.position);
            radius = 0.4f;
            p1 = basePos + Vector3.up * 0.4f;
            p2 = basePos + Vector3.up * 1.2f;
            return;
        }

        Transform t = col.transform;
        Vector3 center = t.TransformPoint(col.center);
        Vector3 s = t.lossyScale;
        int dir = col.direction;
        float axisScale = (dir == 0 ? Mathf.Abs(s.x) : (dir == 1 ? Mathf.Abs(s.y) : Mathf.Abs(s.z)));
        float perpA = (dir == 0 ? Mathf.Abs(s.y) : Mathf.Abs(s.x));
        float perpB = (dir == 2 ? Mathf.Abs(s.y) : Mathf.Abs(s.z));
        radius = col.radius * Mathf.Max(perpA, perpB);
        float heightWorld = Mathf.Max(col.height * axisScale, radius * 2f);
        float halfCylinder = Mathf.Max(0f, (heightWorld * 0.5f) - radius);
        Vector3 axis = (dir == 0 ? t.right : (dir == 1 ? t.up : t.forward));
        p1 = center + axis * halfCylinder;
        p2 = center - axis * halfCylinder;
    }

    public bool IsGrounded(float distanceToGround = 0.3f, float sphereRadius = 0.25f)
    {
        Vector3 origin = follow.monsterReference.transform.position + Vector3.up * 0.2f;
        float castDistance = distanceToGround + 0.2f;

        return Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            LayerMask.GetMask("Ground", "Default")
        );
    }

    public void MoveTowardsFromPoint(Vector3 point, float power, float seconds = 0.25f)
    {
        Vector3 dir = follow.agent.transform.position - point;
        dir.y = 0f;
        dir.Normalize();

        if (power < 0f)
        {
            dir = -dir;
            power = -power;
        }

        Vector3 pushVelocity = dir * power * 0.45f;

#if UNITY_SERVER
        EnqueuePush(pushVelocity, (seconds > 0f ? seconds : 0.2f), follow.agent.transform.position, follow.agent.transform.rotation);
        TriggerPushClientRpc(pushVelocity, (seconds > 0f ? seconds : 0.2f), follow.agent.transform.position, follow.agent.transform.rotation);
#else
        // Correction : remplacement de 'agent' par 'follow.agent'
        EnqueuePush(pushVelocity, (seconds > 0f ? seconds : 0.2f), follow.agent.transform.position, follow.agent.transform.rotation);
#endif
    }

    public void MoveTowardsCaster(PlayerReference caster, float power, float seconds = 0.25f)
    {
        if (caster == null) return;
        MoveTowardsFromPoint(caster.transform.position, power, seconds);
    }

    [ClientRpc]
    public void SyncMonsterPositionClientRpc(Vector3 position)
    {
        if (!follow.physicsMonster.IsPushing && follow.agent.enabled) follow.agent.Warp(position);
        if (!follow.agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(follow.agent.transform.position, out hit, 6f, NavMesh.AllAreas))
            {
                follow.agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning("Aucune position NavMesh trouvée autour du monstre.");
            }
        }
    }
}
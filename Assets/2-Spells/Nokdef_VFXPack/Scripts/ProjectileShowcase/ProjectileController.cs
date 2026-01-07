using Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using PhysicsBasedCharacterController;

public class ProjectileController : MonoBehaviour
{

    public bool readyToDie = false;

    public bool continueToMove = false;

    [SerializeField]
    private Spell spell;

    [SerializeField]
    private float teleguidanceVerticalOffset = 1f;

    public float speed = 100f;  // Vitesse du projectile
    public float projectileSize = 0.5f;  // Taille approximative du projectile
    public LayerMask collisionLayers;  // Les couches avec lesquelles le projectile peut entrer en collision
    private Dictionary<ulong, float> touchedPlayers;
    private float spawnTime;
    private Transform homingTarget;
    private float lastRelockCheck;

    private void Start()
    {
        touchedPlayers = new Dictionary<ulong, float>();
        StartCoroutine(DestroyableAfter());
        spawnTime = Time.time;
        TryAcquireTargetOnSpawn();
        lastRelockCheck = Time.time;
    }

    private IEnumerator DestroyableAfter()
    {
        yield return new WaitForSeconds(0.1f);
        readyToDie = true;
    }

    void Update()
    {
        // Calculer le d�placement du projectile pour cette frame
        float effectiveSpeed = speed;
        // Teleguidance: adjust rotation and optionally override speed
        if (spell != null && spell.useTeleguidance && spell.teleguidanceList != null && spell.teleguidanceList.Count > 0)
        {
            float t = Time.time - spawnTime;
            Game.TeleguidanceSegment active = null;
            for (int i = 0; i < spell.teleguidanceList.Count; i++)
            {
                var seg = spell.teleguidanceList[i];
                if (t >= seg.start && (seg.end <= 0f || t < seg.end))
                {
                    active = seg; break;
                }
            }

            if (active != null)
            {
                if (active.speed >= 0f)
                    effectiveSpeed = active.speed;

                if (active.enableHoming)
                {
                    // Relock logic: if target lost (dead or out of range), try reacquire periodically
                    if (spell.relockWhenLost && Time.time - lastRelockCheck >= Mathf.Max(0.01f, spell.relockCheckInterval))
                    {
                        if (IsTargetInvalid())
                        {
                            AcquireTarget(transform.position, transform.forward);
                        }
                        lastRelockCheck = Time.time;
                    }

                    Vector3 desiredDir = transform.forward;
                    if (homingTarget != null)
                    {
                        Vector3 targetPos = homingTarget.position + Vector3.up * teleguidanceVerticalOffset; // Aim slightly above feet
                        desiredDir = (targetPos - transform.position);
                        if (active.ignoreVertical) desiredDir.y = 0f;
                        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();
                    }
                    else
                    {
                        Transform aim = null;
                        if (active.useCasterAim && spell.casterRef != null && spell.casterRef.playerShooting != null)
                            aim = spell.casterRef.playerShooting.castProjectilePosition;
                        if (aim == null && (active.useCasterForward || active.useCasterAim) && spell.GetCaster() != null)
                            aim = spell.GetCaster().transform;
                        if (aim != null)
                            desiredDir = aim.forward;
                    }

                    float maxRad = Mathf.Deg2Rad * Mathf.Max(0f, active.turnRateDeg) * Time.deltaTime;
                    if (active.ignoreVertical)
                    {
                        desiredDir.y = 0f;
                        Vector3 current = transform.forward; current.y = 0f;
                        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();
                        if (current.sqrMagnitude > 0.0001f) current.Normalize();
                        Vector3 newDir = Vector3.RotateTowards(current, desiredDir, maxRad, float.MaxValue);
                        if (newDir.sqrMagnitude > 0.0001f)
                            transform.rotation = Quaternion.LookRotation(new Vector3(newDir.x, 0f, newDir.z));
                    }
                    else
                    {
                        Vector3 newDir = Vector3.RotateTowards(transform.forward, desiredDir, maxRad, float.MaxValue);
                        if (newDir.sqrMagnitude > 0.0001f)
                            transform.rotation = Quaternion.LookRotation(newDir);
                    }
                }
            }
        }


        Vector3 moveDistance = transform.forward * effectiveSpeed * Time.deltaTime;
        float moveMagnitude = moveDistance.magnitude;

        // Effectuer un SphereCast pour d�tecter des collisions sur le chemin du projectile
        if (Physics.SphereCast(transform.position, projectileSize, transform.forward, out RaycastHit hit, moveMagnitude, collisionLayers))
        {
            // Si une collision est d�tect�e, on g�re l'impact
            if (hit.collider.gameObject.Equals(spell.GetCaster()))
            {
                transform.position += moveDistance;
            }
            else
            {
                OnHit(hit);
                if (!continueToMove)
                {
                    Destroy(gameObject, 0.5f);
                }
                else
                {
                    transform.position += moveDistance;
                }
            }
        }
        else
        {
            // Si pas de collision, le projectile continue de se d�placer
            transform.position += moveDistance;
        }
    }

    private void TryAcquireTargetOnSpawn()
    {
        if (spell == null || !spell.useTeleguidance || !spell.lockTargetOnSpawn)
            return;

        Transform aim = null;
        if (spell.casterRef != null && spell.casterRef.playerShooting != null && spell.casterRef.playerShooting.castProjectilePosition != null)
            aim = spell.casterRef.playerShooting.castProjectilePosition;
        if (aim == null && spell.GetCaster() != null)
            aim = spell.GetCaster().transform;

        Vector3 origin = transform.position;
        Vector3 forward = aim != null ? aim.forward : transform.forward;
        AcquireTarget(origin, forward);
    }

    private bool IsAttackableCategory(PlayerReference target)
    {
        if (target == null || spell == null || spell.casterRef == null) return false;

        var casterRef = spell.casterRef;
        var casterIsMonster = casterRef.playerClasses != null && casterRef.playerClasses.isMonster;
        var targetIsMonster = target.playerClasses != null && target.playerClasses.isMonster;

        // Respect caster preferences
        if (casterRef.playerStatistics != null)
        {
            if (targetIsMonster && casterRef.playerStatistics.NotAttackMonsters) return false;
            if (!targetIsMonster && casterRef.playerStatistics.NotAttackPlayers) return false;
        }

        // If caster is monster, prefer players as targets; if caster is player, allow monsters or enemy players
        if (casterIsMonster)
        {
            if (targetIsMonster) return false;
        }
        else
        {
            // Player caster: exclude self already, allow both monsters and players
            // Potential place to exclude group/party allies if group data is available
        }

        // Exclude heart if caster cannot attack it
        if (target.gameObject.layer == 12 && casterRef.playerStatistics != null && casterRef.playerStatistics.NotAttackHeart)
            return false;

        return true;
    }

    private bool IsTargetInvalid()
    {
        if (homingTarget == null) return true;
        var pr = homingTarget.GetComponentInParent<PlayerReference>();
        if (pr == null) return true;
        if (pr.playerStatistics != null && pr.playerStatistics.isDead) return true;
        float range = Mathf.Max(0.1f, spell.lockRange);
        if ((homingTarget.position - transform.position).sqrMagnitude > range * range) return true;
        return false;
    }

    private void AcquireTarget(Vector3 origin, Vector3 forward)
    {
        float range = Mathf.Max(0.1f, spell.lockRange);
        float radius = Mathf.Max(0.01f, spell.raycastAutoTargetWidth);

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, forward, range, ~0, QueryTriggerInteraction.Collide);
        PlayerReference best = null;
        float bestDist = float.MaxValue;
        int bestPriority = int.MaxValue;

        foreach (var h in hits)
        {
            var pr = h.collider.GetComponentInParent<PlayerReference>();
            if (pr == null) continue;
            if (spell.casterRef != null && pr == spell.casterRef) continue;
            if (pr.playerStatistics != null && pr.playerStatistics.isDead) continue;
            if (!IsAttackableCategory(pr)) continue;

            bool isMonster = pr.playerClasses != null && pr.playerClasses.isMonster;
            int priority = 0;
            if (spell.preferPlayersForLock)
                priority = isMonster ? 1 : 0;

            if (priority < bestPriority || (priority == bestPriority && h.distance < bestDist))
            {
                bestPriority = priority;
                bestDist = h.distance;
                best = pr;
            }
        }

        homingTarget = best != null ? best.transform : null;
    }








    // Gestion des collisions avec des objets
    private void OnHit(RaycastHit hit)
    {




#if UNITY_SERVER
        // Afficher des informations de collision
        if (spell == null)
            return;
        if (spell.GetCaster() != hit.collider.gameObject) {

            if (hit.collider.gameObject.layer == 3 || hit.collider.gameObject.layer == 7 || (spell.casterRef.playerClasses.isMonster && hit.collider.gameObject.layer == 9) || hit.collider.gameObject.layer == 12) {
                PlayerReference stats = hit.collider.GetComponent<PlayerReference>();
                if ((stats.playerDash?.isDashing ?? false) || (stats.playerStatistics?.isInvincible ?? false))
                    return;
                PlayerReference statsp = spell.casterRef;

                // Si nous n'avons pas � attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
                if (statsp.playerStatistics.NotAttackMonsters && hit.collider.gameObject.layer == 7)
                    return;
                if (statsp.playerStatistics.NotAttackPlayers && hit.collider.gameObject.layer == 3)
                    return;
                if (statsp.playerStatistics.NotAttackPlayers && hit.collider.gameObject.layer == 9)
                    return;
                if (statsp.playerStatistics.NotAttackHeart && hit.collider.gameObject.layer == 12)
                    return;
                // Si on doit taper le joueur adverse une seule fois.
                NetworkObject netOther = hit.collider.GetComponent<NetworkObject>();

                if (spell.spellDamageOnce) {
                    // Et que le joueur n'est pas d�j� dans la liste des objets d�j� touch�s.
                    if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId)) {
                        // On l'ajoute des les objets touch�s
                        touchedPlayers.Add(netOther.NetworkObjectId, Time.time);

                        // On r�cup�re et on lui applique les d�g�ts, effets, etc.
                        stats.playerStatistics.SetAttacker(spell.GetCaster());
                        SpellDamageParticle.CheckStatePlayer(stats, spell);
                        if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !stats.playerClasses.isMonster)
                        {
                            CharacterManager manager = stats.GetComponent<CharacterManager>();
                            if (manager != null)
                            {
                                manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
                            }
                        }
                    }
                }
                // Si on tape un joueur qu'on peut taper plusieurs fois
                else {
                    // Et que le joueur n'est pas d�j� dans la liste des objets d�j� touch�s.
                    if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId)) {
                        // On r�cup�re et on lui applique les d�g�ts, effets, etc.
                        stats.playerStatistics.SetAttacker(spell.GetCaster());

                        SpellDamageParticle.CheckStatePlayer(stats, spell);
                        if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !stats.playerClasses.isMonster)
                        {
                            CharacterManager manager = stats.GetComponent<CharacterManager>();
                            if (manager != null)
                            {
                                manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
                            }
                        }
                    }
                }
            }
        }
#else
        if (spell == null)
            return;
        if (spell.GetCaster() != hit.collider.gameObject)
        {
            PlayerReference casterRef = spell.casterRef;
            if (hit.collider.gameObject.layer != 3 && hit.collider.gameObject.layer != 7 && (spell.GetCaster().gameObject.layer == 9 || hit.collider.gameObject.layer != 9) && hit.collider.gameObject.layer != 12)
            {
                return;
            }


            PlayerReference otherReference = hit.collider.GetComponent<PlayerReference>();

            if ((otherReference.playerDash?.isDashing ?? false) || (otherReference.playerStatistics?.isInvincible ?? false))
                return;
            // Si nous n'avons pas � attaquer des monstres ou des joueurs et que la cible est un monstre ou un monstre: NOP
            if (casterRef.playerStatistics.NotAttackMonsters && hit.collider.gameObject.layer == 7)
                return;
            if (casterRef.playerStatistics.NotAttackPlayers && hit.collider.gameObject.layer == 3)
                return;
            if (casterRef.playerStatistics.NotAttackPlayers && hit.collider.gameObject.layer == 9)
                return;
            if (casterRef.playerStatistics.NotAttackHeart && hit.collider.gameObject.layer == 12)
                return;
            Debug.Log("Le sort touche");
            if (spell.spellDamageOnce)
            {
                NetworkObject netOther = hit.collider.GetComponent<NetworkObject>();
                // Et que le joueur n'est pas d�j� dans la liste des objets d�j� touch�s.
                if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                {
                    // On l'ajoute des les objets touch�s
                    touchedPlayers.Add(netOther.NetworkObjectId, Time.time);


                    if (otherReference.IsLocalPlayer)
                    {
                        SoundManager.Instance.Play2D("hitted");
                    }
                    if (casterRef.IsLocalPlayer)
                    {
                        SoundManager.Instance.Play2D("hit");
                        cooldownUI.instance.PlayCursorHitAnimation();
                    }
                    SpellDamageParticle.CheckStatePlayer(otherReference, spell);
                    if (otherReference.playerStatistics.playerStatData.health > 0f)
                    {
                        spell.SpawnTargetPrefabIfAllowed(otherReference.gameObject);
                    }
                    if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !otherReference.playerClasses.isMonster)
                    {
                        CharacterManager manager = otherReference.GetComponent<CharacterManager>();
                        if (manager != null)
                        {
                            manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
                        }
                    }
                }
            }
            else
            {
                NetworkObject netOther = hit.collider.GetComponent<NetworkObject>();
                // Et que le joueur n'est pas d�j� dans la liste des objets d�j� touch�s.
                if (!touchedPlayers.ContainsKey(netOther.NetworkObjectId))
                {
                    // On r�cup�re et on lui applique les d�g�ts, effets, etc.
                    otherReference.playerStatistics.SetAttacker(spell.GetCaster());

                    SpellDamageParticle.CheckStatePlayer(otherReference, spell);
                    if (otherReference.playerStatistics.playerStatData.health > 0f)
                    {
                        spell.SpawnTargetPrefabIfAllowed(otherReference.gameObject);
                    }
                    if (spell.jumpHeightFactor > 0 && spell.jumpHeightFactor != 1 && !otherReference.playerClasses.isMonster)
                    {
                        CharacterManager manager = otherReference.GetComponent<CharacterManager>();
                        if (manager != null)
                        {
                            manager.AddJumpVelocityFactor(spell.jumpHeightFactor, spell.jumpHeightSeconds);
                        }
                    }
                }
            }
        }
#endif
    }
    //Destroy(gameObject);
    public void SpawnSubFX(GameObject fx, Transform spawnPos, float delay)
    {
        GameObject instance = Instantiate(fx, spawnPos.position, spawnPos.rotation);
        Destroy(instance, delay);
    }

    private void OnDestroy()
    {
        // On enl�ve l'�tat d'avalement aux joueurs
        if (spell?.targetBehind?.Count > 0)
        {
            foreach (PlayerReference player in spell.targetBehind.ToList())
            {
                player.playerStatistics.isBehindWho = null;
                player.follow.GetAgent().enabled = true;
            }
        }
        StopAllCoroutines();
    }
}

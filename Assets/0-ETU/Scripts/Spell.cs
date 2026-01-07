using Cinemachine;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace Game
{

    [Serializable]
    public class TeleguidanceSegment
    {
        [Header("Active time window [seconds]")]
        public float start = 0f;
        public float end = 0f;

        [Header("Enable homing during this window")]
        public bool enableHoming = true;

        [Header("Override speed (-1 = keep base)")]
        public float speed = -1f;

        [Header("Max turn rate (deg/sec)")]
        public float turnRateDeg = 360f;

        [Header("Direction source")]
        public bool useCasterAim = true;       // Use casterShooting.castProjectilePosition forward if available
        public bool useCasterForward = false;  // Otherwise use caster transform forward

        [Header("Plane constraints")]
        public bool ignoreVertical = false;    // If true, keep homing in XZ plane only
    }

    [Serializable]
    public class PushType
    {
        public float pushStartDelay;
        public float pushEndDelay;
        public Vector3 pushPower;
        public bool stopOnGrounded;
    }

    [Serializable]
    public class SpawnType
    {
        public GameObject spawnObject;
        public float spawnDelay;
        public bool isNetworkObject;
        public bool destroyIfSpellDestroy;
    }

    [Serializable]
    public class CasterControlSegment
    {
        [Header("Active time window [seconds] from spawn")]
        public float start = 0f;
        public float end = 0f; // If end < start or < 0 -> until spell destroy

        [Header("Movement speed multiplier during the window")]
        public float moveSpeedFactor = 1f;

        [Header("Allow abilities during the window")]
        public bool allowJump = true;
        public bool allowDash = true;
    }


    public class Spell : MonoBehaviour
    {

        [Header("0/ Front Skill, 1: AOE")]
        public int spellType; // 0: Front Skill , 1: AOE
        public GameObject aoeCircle;

        public int spellRange; // Pour les monstres, la distance ï¿½ laquelle ils envoient leur sort x
        public float spellDamage;

        public bool spellDamageOnce = false;

        public float spellDamagePerSecond = 1.0f;

        [Header("Healing settings (like damage Once/PerSecond)")]
        public float healValue = 0f;
        public bool healOnce = false;
        public float healPerSecond = 1.0f;

        public enum HealTargets
        {
            None = 0,
            AllyOnly = 1,
            AllyOrEnemy = 2,
            SelfOnly = 3,
            SelfAndAlly = 4
        }
        [Header("Who can be healed by this spell?")]
        public HealTargets healTargets = HealTargets.None;

        public bool cancelSpellIfStunned;
        // Plus tard, crï¿½er plusieurs variables:

        // spellhittingOne: est-ce que le sort frappe 1 seule fois ?

        // SI LE SORT FRAPPE 1 SEULE FOIS:
        // spellhittingSecond : dans combien le sort frappe 1 seule fois ?

        // CES VARIABLES NE SONT UTILES QUE SI LE SORT NE FRAPPE PAS 1 SEULE FOIS :
        // spellStartHiting : dans combien de seconde le sort commence ï¿½ frapper;
        // spellEndHiting : dans combien de seconde le sort finit ï¿½ frapper.
        // spellHitBySecond: combien de hit par seconde on enlï¿½ve aux ennemies.
        // spellHitNumberForSameEnemy: combien de hit pour un mï¿½me ennemi.


        public float spellCooldown;
        public float spellMinCooldown = 6f;
        public float durationWhileCancelAvailable;

        public bool followCaster;
        public bool followRotationCaster;

        [Header("Utiliser la rotation de castProjectilePosition au lieu de celle du caster")]
        public bool followProjectileRotation;
        [Header("Forcer la rotation du sort à l'identité (0,0,0)")]
        public bool forceIdentityRotation = false;

        [Header("Teleguidance (Homing) Configuration")]
        [Tooltip("Master switch to allow projectile teleguidance.")]
        public bool useTeleguidance = false;
        [Tooltip("Sequence of homing/speed windows. Times are relative to the projectile spawn.")]
        public List<TeleguidanceSegment> teleguidanceList = new List<TeleguidanceSegment>();
        [Header("Caster control timeline (affects the caster)")]
        [Tooltip("Sequence applied to the caster over the spell lifetime: movement multiplier + jump/dash gating.")]
        public List<CasterControlSegment> casterControlTimeline = new List<CasterControlSegment>();

        [Header("Homing Targeting")]
        [Tooltip("Acquire a target when the projectile spawns.")]
        public bool lockTargetOnSpawn = true;
        [Tooltip("Maximum lock range in meters.")]
        public float lockRange = 30f;
        [Tooltip("Field of view in degrees for initial lock (centered on caster aim/forward).")]
        public float lockFOVDeg = 60f;
        [Tooltip("If no target in FOV, allow nearest within range anyway.")]
        public bool fallbackNearestInRange = true;
        [Tooltip("Radius of the initial spherecast for auto-target (m)")]
        public float raycastAutoTargetWidth = 0.75f;
        [Tooltip("Prefer locking players over monsters when both are detected")]
        public bool preferPlayersForLock = true;
        [Tooltip("If true, will try to reacquire a new target when the current one dies or leaves range.")]
        public bool relockWhenLost = true;
        [Tooltip("How often (seconds) we attempt to relock when target lost.")]
        public float relockCheckInterval = 0.15f;


        public float durationBeforeDestroy;
        public float durationBeforeEffect = 0f;

        // Gestion des animations
        public bool isAnimationParameter;
        public float durationBeforeRemoveParameter;
        public bool removeAnimationParameterOnGrounded;

        public bool blockOtherSpell;
        public bool overrideBlocking;

        public bool isAnimationPlay;
        public string animationName;

        // S'il s'agit d'un animation parameter: il s'enlevera quand le parameter sera enlevï¿½.
        // S'il s'agit d'une animation Play, choisir la durï¿½e avec:
        [Header("spellPriority / spellPriority: bloque les animations de hit de marche, de saut, etc : 99% des cas, cochez!")]
        public bool spellPriority;
        [Header("Combien de secondes on protï¿½ge ?")]
        [Header("Cela permet notamment de bloquer les animations de marche, fall, jump pendant l'execution de l'animation des sorts")]
        [Header("Bien ajustï¿½ le temps car cela bloque le cast des combos: ne pas abusï¿½ des secondes")]
        public float spellPrioritySeconds;


        [Header("Todo: blockSpells: bloquer le cast des sorts")]

        [Header("castPrefab: si != null, cela va ï¿½tre spawn sur le personnage")]
        [SerializeField]
        private GameObject castPrefab;

        [Header("targetPrefab: si != null, cela va ï¿½tre spawn sur le personnage")]
        [SerializeField]
        private GameObject targetPrefab;

        [Header("castPosition: 0 : position joueur, 1: position des mains")]
        public int castPosition = 0;

        [Header("blockMoveSeconds: bloque les dï¿½placements du joueur (si parameter: s'enleve avec : durationBeforeRemoveParameter) (si animation play: blockMoveSeconds")]
        public float blockMoveSeconds;

        [Header("blockCastingSpellsSeconds: bloque le cast de nouveaux sorts du joueur")]
        public float blockCastingSpellsSeconds;

        [Header("Transformation: annuler/dÃ©truire les effets quand le lanceur se transforme")]
        public bool cancelEffectsOnTransformation;
        public bool destroyEffectOnTransformation;

        [Header("Cast (barre de progression avant le lancement)")]
        public bool useCastTime;
        public float castDuration = 0f;
        public bool cancelCastOnDash = true;
        public bool cancelCastOnMove = true;
        [Tooltip("Seuil d'input de déplacement (0-1) avant d'annuler sur mouvement. <= 0 pour utiliser le seuil par défaut.")]
        public float cancelCastMoveMinSpeed = 0f;
        public bool cancelCastOnTransformation = true;
        public bool cancelCastOnAutoAttack = true;
        public bool cancelCastOnOtherSpell = true;
        [Tooltip("Annule le cast si le lanceur n'est pas au sol pendant la barre de cast.")]
        public bool cancelCastOnAir = true;
        [Tooltip("Annule le cast si le lanceur est au sol pendant la barre de cast.")]
        public bool cancelCastOnGround = false;
        [Tooltip("Annule le cast si le lanceur appuie sur Jump pendant la barre de cast.")]
        public bool cancelCastOnJump = true;
        [Tooltip("Annule le cast si le lanceur subit un contr\u00f4le (stun/freeze/sleep/para/server block) pendant la barre de cast.")]
        public bool cancelCastOnCrowdControl = true;
        [Tooltip("Annule le cast si le lanceur subit des d\u00e9g\u00e2ts pendant la barre de cast.")]
        public bool cancelCastOnHit = true;
        [Tooltip("Annule le cast si le lanceur est repouss\u00e9/pouss\u00e9 pendant la barre de cast.")]
        public bool cancelCastOnPush = true;
        public string castAnimationName;
        [Tooltip("Prefab affichÃ© pendant le cast (instanciÃ© au dÃ©but du cast, dÃ©truit si cancel ou fin)")]
        public GameObject castVfxPrefab;

        [Header("blockCastingOnGrounded: bloque le cast du sort par terre")]
        public bool blockCastingOnGrounded;

        public bool removeOtherSpellParameter;


        // Sort des mages en gï¿½nï¿½ral, est:
        // public bool isSpellForward;

        // est-ce qu'on utilise la position en face comme position: (c'est utile parfois si le projectile se pousse tout seul)
        [Header("Permet que le sort utilise la rotation du joueur. Pour tout ce qui dï¿½pend du sens du joueur")]
        public bool isProjectilePosition;

        // Est-ce qu'on lance l'effet lï¿½ ou on va le pousser plus tard ou pas ? dans a nouvelle position
        public bool launchEffectToNewPosition;

        // Gestion des mouvements
        public float pushTargetFrontSeconds;
        public float pushTargetFrontPower;

        public float pushTargetBackSeconds;
        public float pushTargetBackPower;

        public float pushTargetJumpSeconds;
        public float pushTargetJumpPower;

        public float pushTargetDownSeconds;
        public float pushTargetDownPower;

        public List<PushType> pushCasterList;
        public List<PushType> pushTargetList;

        public float targetInAirSeconds;
        public float casterInAirSeconds;

        [Header("--------------------------")]
        [Header("Target effects (stun, freeze, paralyze, sleep, etc)")]
        [SerializeField]
        private float stunSeconds;
        [SerializeField]
        private float freezeSeconds;
        [SerializeField]
        private float sleepSeconds;
        [SerializeField]
        private float paraSeconds;
        [SerializeField]
        private float stunAirSeconds;

        [Tooltip("Modifier en rï¿½duisant la movement speed de la cible")]
        public float movementSpeedFactor = 1;
        public float movementSpeedSeconds;

        [Tooltip("Multiplicateur appliquÃ© Ã  la hauteur de saut de la cible")]
        public float jumpHeightFactor = 1;
        public float jumpHeightSeconds;

        [Tooltip("Permet de coller un joueur ï¿½ la position ci jointe liï¿½ ï¿½ ce joueur")] public Vector3 keepTargetBehind = default;
        [HideInInspector] public List<PlayerReference> targetBehind;

        [Tooltip("Durï¿½e d'invincibilitï¿½")]
        public float invincibleTime;

        [Header("Le sort ne peut pas Ãªtre interrompu une fois lancÃ©")]
        public bool cannotBeCancelled = false;

        public List<SpawnType> spawnThisOnStart;
        public List<SpawnType> spawnThisOnCollision;
        public List<SpawnType> spawnThisOnRayCast;
        [HideInInspector] public List<SpawnType> spawnedObjects;

        // --- Caster control timeline runtime tracking ---
        public Coroutine casterControlCoroutine;
        private readonly List<List<float>> _appliedSpeedFactors = new List<List<float>>();
        private readonly List<List<float>> _appliedJumpBlocks = new List<List<float>>();
        private readonly List<List<float>> _appliedDashBlocks = new List<List<float>>();
        private bool casterTimelineCancelled = false;
        private int _casterSpellIndex = -1;
        public int CasterSpellIndex => _casterSpellIndex;

        [Header("--------------------------")]
        // Annulation du sort:
        public bool isCancel;
        // Le bouton du sort (celui qu'il faut avoir appuyï¿½ pour cancel le sort)
        public string buttonName;

        // Visuel UI
        public Texture avatar;

        [SerializeField] private GameObject caster;
        public PlayerReference casterRef;

        public float timeStarted;

        private Animator casterAnimator;

        public float StunSeconds { get => stunSeconds; set => stunSeconds = value; }
        public float FreezeSeconds { get => freezeSeconds; set => freezeSeconds = value; }
        public float SleepSeconds { get => sleepSeconds; set => sleepSeconds = value; }
        public float ParaSeconds { get => paraSeconds; set => paraSeconds = value; }
        public float StunAirSeconds { get => stunAirSeconds; set => stunAirSeconds = value; }
        public GameObject TargetPrefab { get => targetPrefab; set => targetPrefab = value; }
        [NonSerialized]
        private Dictionary<int, float> targetPrefabSpawnTracker = new Dictionary<int, float>();

        private const float targetPrefabLifetime = 3f;

        public GameObject SpawnTargetPrefabIfAllowed(GameObject target)
        {
            if (target == null || targetPrefab == null)
                return null;

            int targetKey = target.GetInstanceID();
            float now = Time.time;

            // Use corresponding gating depending on whether we are using damage tick or heal tick
            bool once = spellDamageOnce || (healTargets != HealTargets.None && healOnce);
            float tickInterval = (healTargets != HealTargets.None && !healOnce) ? Mathf.Max(0.001f, healPerSecond) : Mathf.Max(0.001f, spellDamagePerSecond);

            if (once)
            {
                if (targetPrefabSpawnTracker.ContainsKey(targetKey))
                {
                    return null;
                }
            }
            else
            {
                if (targetPrefabSpawnTracker.TryGetValue(targetKey, out float lastSpawnTime))
                {
                    if (now - lastSpawnTime < tickInterval)
                    {
                        return null;
                    }
                }
            }

            targetPrefabSpawnTracker[targetKey] = now;

            GameObject instance = Instantiate(targetPrefab, target.transform.position, target.transform.rotation);
            Buff buff = instance.GetComponent<Buff>();
            if (buff != null)
            {
                buff.SetCaster(target);
            }

            Destroy(instance, targetPrefabLifetime);
            return instance;
        }

        public GameObject CastPrefab { get => castPrefab; set => castPrefab = value; }

        public CinemachineImpulseSource impulseSource;
        private CinemachineImpulseManager.ImpulseEvent impulseEvent;


        [HideInInspector] public HashSet<ulong> pushedTargets = new HashSet<ulong>();


        private void Start()
        {
            timeStarted = Time.time;
            targetBehind = new List<PlayerReference>();
            spawnedObjects = new List<SpawnType>();


#if UNITY_SERVER
            SpawnObjectsPeriodically();
            // Server-side self-heal handling for self-targeted heal spells
            if (healValue > 0f && (healTargets == HealTargets.SelfOnly || healTargets == HealTargets.SelfAndAlly))
            {
                StartCoroutine(ApplySelfHeal());
            }
#endif
        }

#if UNITY_SERVER
        private IEnumerator ApplySelfHeal()
        {
            // Optional delay before effect
            if (durationBeforeEffect > 0f)
                yield return new WaitForSeconds(durationBeforeEffect);

            if (casterRef == null)
                yield break;

            if (healOnce)
            {
                casterRef.playerStatistics.ApplyHeal(casterRef.gameObject, healValue, casterRef);
                yield break;
            }

            float interval = Mathf.Max(0.001f, healPerSecond);
            while (true)
            {
                if (casterRef == null) yield break;
                casterRef.playerStatistics.ApplyHeal(casterRef.gameObject, healValue, casterRef);
                yield return new WaitForSeconds(interval);
            }
        }
#endif

        public void SetCaster(GameObject casterObject, int index, Boolean isAuto = false)
        {
            caster = casterObject;
            casterRef = caster.GetComponent<PlayerReference>();
            _casterSpellIndex = index;

            if (!isAuto)
            {
                var playerClasses = caster.GetComponent<PlayerClasses>();
                if (casterRef.PlayerReincarnation != null && casterRef.PlayerReincarnation.IsReincarnation)
                {
                    playerClasses.reincarnationRealSpells[index] = this;
                }
                else
                {
                    playerClasses.humanRealSpells[index] = this;
                }
            }
            if (caster.GetComponent<NetworkObject>().IsLocalPlayer)
            {
                if (impulseSource != null) impulseSource.GenerateImpulse();
            }

            if (removeOtherSpellParameter)
            {
                var realSpellsArray = casterRef.PlayerReincarnation != null && casterRef.PlayerReincarnation.IsReincarnation
                    ? casterRef.playerClasses.reincarnationRealSpells
                    : casterRef.playerClasses.humanRealSpells;
                for (int i = 0; i < realSpellsArray.Count(); i++)
                {
                    if (realSpellsArray[i] != null) Debug.Log("Check to remove parameter for " + i + " " + realSpellsArray[i] + " " + realSpellsArray[i].isAnimationParameter);
                    if (index != i && realSpellsArray[i] != null && realSpellsArray[i].isAnimationParameter)
                    {
                        casterRef.playerShooting.CancelSpell(i, true);
                    }
                }
            }
        }

        public IEnumerator ApplyCasterControlTimeline(PlayerReference playerReference)
        {
            // Only apply locally to the caster's client
            if (playerReference == null || playerReference.playerMovement == null)
                yield break;

            if (!playerReference.networkObject.IsLocalPlayer)
                yield break;

            casterTimelineCancelled = false;
            // Sort by start time
            var segments = casterControlTimeline.OrderBy(s => s.start).ToList();
            float baseline = Time.time; // use actual coroutine start time as baseline
            float cursor = 0f;

            foreach (var seg in segments)
            {
                if (casterTimelineCancelled) yield break;
                float wait = Mathf.Max(0f, seg.start - cursor);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);

                if (casterTimelineCancelled) yield break;
                float duration;
                if (seg.end < 0f || seg.end <= seg.start)
                {
                    // Until destroy -> if a finite lifetime exists, use remaining time; otherwise, keep until explicit cancel
                    if (durationBeforeDestroy > 0f)
                        duration = Mathf.Max(0f, (durationBeforeDestroy - (Time.time - baseline)));
                    else
                        duration = Mathf.Infinity;
                }
                else
                {
                    duration = Mathf.Max(0f, seg.end - seg.start);
                }

                if (casterTimelineCancelled) yield break;
                // Apply movement speed factor if not 1
                if (Mathf.Abs(seg.moveSpeedFactor - 1f) > 0.0001f)
                {
                    var entry = new List<float> {
                        seg.moveSpeedFactor,
                        duration,
                        Time.time
                    };
                    playerReference.playerMovement.movementSpeedFactors.Add(entry);
                    _appliedSpeedFactors.Add(entry);
                }

                // Apply jump/dash blockers via PlayerMovement helper lists
                if (!seg.allowJump)
                {
                    var jentry = new List<float> { duration, Time.time };
                    playerReference.playerMovement.jumpBlockers.Add(jentry);
                    _appliedJumpBlocks.Add(jentry);
                }
                if (!seg.allowDash)
                {
                    var dentry = new List<float> { duration, Time.time };
                    playerReference.playerMovement.dashBlockers.Add(dentry);
                    _appliedDashBlocks.Add(dentry);
                }

                // Advance cursor to seg.start for next delta wait
                cursor = seg.start;
            }
        }

        public void CancelCasterTimelineImmediate(PlayerReference player)
        {
            casterTimelineCancelled = true;
            if (player != null && player.playerMovement != null)
            {
                foreach (var e in _appliedSpeedFactors) player.playerMovement.movementSpeedFactors.Remove(e);
                foreach (var e in _appliedJumpBlocks) player.playerMovement.jumpBlockers.Remove(e);
                foreach (var e in _appliedDashBlocks) player.playerMovement.dashBlockers.Remove(e);
            }
            _appliedSpeedFactors.Clear();
            _appliedJumpBlocks.Clear();
            _appliedDashBlocks.Clear();
        }

        public void StopCasterTimelineAndClearEffects()
        {
            if (casterControlCoroutine != null)
            {
                try { StopCoroutine(casterControlCoroutine); } catch { }
                casterControlCoroutine = null;
            }

            if (casterRef != null && casterRef.playerMovement != null)
            {
                // Remove any still-active entries we added
                foreach (var e in _appliedSpeedFactors)
                {
                    casterRef.playerMovement.movementSpeedFactors.Remove(e);
                }
                foreach (var e in _appliedJumpBlocks)
                {
                    casterRef.playerMovement.jumpBlockers.Remove(e);
                }
                foreach (var e in _appliedDashBlocks)
                {
                    casterRef.playerMovement.dashBlockers.Remove(e);
                }

                // Safety net: if somehow still blocked, force-clear blockers added via timeline
                if (_appliedJumpBlocks.Count > 0 && casterRef.playerMovement.IsJumpBlocked())
                {
                    casterRef.playerMovement.jumpBlockers.Clear();
                }
                if (_appliedDashBlocks.Count > 0 && casterRef.playerMovement.IsDashBlocked())
                {
                    casterRef.playerMovement.dashBlockers.Clear();
                }
            }

            _appliedSpeedFactors.Clear();
            _appliedJumpBlocks.Clear();
            _appliedDashBlocks.Clear();
        }

        private void LateUpdate()
        {
            if (caster)
            {
                if (forceIdentityRotation)
                {
                    // Garde la rotation à l'identité même si le caster tourne
                    transform.rotation = Quaternion.identity;
                }
                else
                {
                    if (followProjectileRotation && casterRef.playerShooting != null && casterRef.playerShooting.castProjectilePosition != null)
                    {
                        // Utiliser la rotation de castProjectilePosition
                        transform.rotation = casterRef.playerShooting.castProjectilePosition.rotation;
                    }
                    else if (followRotationCaster)
                    {
                        // Sinon, suivre la rotation du caster
                        transform.rotation = casterRef.gameObject.transform.rotation;
                    }
                }

                if (followCaster)
                {
                    // Suivre la position du caster
                    transform.position = caster.transform.position;
                }
            }
        }
        private void Update()
        {

            if (isAnimationParameter)
            {
                if (caster == null) { Destroy(gameObject); }

                if (!casterAnimator)
                {
                    casterAnimator = casterRef.playerClasses.animator;
                }
                if (casterRef.playerClasses.isMonster)
                {
                    if (casterAnimator.GetBool("isWalking"))
                    {
                        casterRef.playerClasses.animator.SetBool("isWalking", false);
                        //caster.GetComponent<PlayerMovement>().ApplyAnimationMonsterClientRpc(classEntity.gameObject, "isWalking", false);
                    }
                }
                else
                {
                    if (casterRef.characterActor.IsGrounded && removeAnimationParameterOnGrounded)
                    {
                        casterRef.playerClasses.animator.SetBool(animationName, false);
                        Destroy(gameObject, 2f);
                    }
                }
                // On gï¿½re maintenant les dï¿½placements avec l'Avatar Mask, pas besoin de dï¿½sactivï¿½)
                /*                    else
                                    {
                                        caster.GetComponent<PlayerMovement>().ApplyAnimationClientRpc("isWalking", false);
                                    }*/
                //}
            }
            // If spell got canceled, stop any ongoing slowdown timeline immediately
            if (_casterSpellIndex >= 0 && casterRef != null && casterRef.playerClasses != null)
            {
                var cancels = casterRef.playerClasses.cancelSpells;
                if (cancels != null && _casterSpellIndex < cancels.Length && cancels[_casterSpellIndex])
                {
                    StopCasterTimelineAndClearEffects();
                }
            }
            if (Time.time > durationBeforeDestroy + timeStarted)
            {
                Destroy(gameObject);
            }
            /*            else
                        {
                            switch (spellOrder)
                            {
                                case 0:
                                    if (isCancel && InputManager.inputActions.Player.Spell1.phase.ToString() == "Phase")
                                    {
                                        gameObject.GetComponent<NetworkObject>().Despawn(true);
                                    }
                                    break;
                                case 1:
                                    if (isCancel && InputManager.inputActions.Player.Spell2.phase.ToString() == "Phase")
                                    {
                                        gameObject.GetComponent<NetworkObject>().Despawn(true);
                                    }
                                    break;
                                case 2:
                                    if (isCancel && InputManager.inputActions.Player.Spell3.phase.ToString() == "Phase")
                                    {
                                        gameObject.GetComponent<NetworkObject>().Despawn(true);
                                    }
                                    break;
                                case 3:
                                    if (isCancel && InputManager.inputActions.Player.Spell4.phase.ToString() == "Phase")
                                    {
                                        gameObject.GetComponent<NetworkObject>().Despawn(true);
                                    }
                                    break;
                            }
                        }*/
        }

        public string GetCasterName()
        {
            if (caster)
                return caster.name;
            return null;
        }

        public GameObject GetCaster()
        {
            if (caster)
                return caster;
            return null;
        }
        /*    private void FixedUpdate()
            {
                if (isSpellForward)
                {
                    transform.Translate(Vector3.forward * 0.5f);
                }
            }*/

        private void OnDestroy()
        {
            // Stop & clear any caster timeline effects (also covers cancel)
            CancelCasterTimelineImmediate(casterRef);
            // Supprimer les objets spawnÃ©s si nÃ©cessaire
            if (spawnedObjects.Count != 0)
            {
                foreach (SpawnType spawnObj in spawnedObjects)
                {
                    if (spawnObj.destroyIfSpellDestroy)
                    {
                        Destroy(spawnObj.spawnObject);
                    }
                }
            }

            // Si ce sort bloquait les autres sorts, lever le blocage
            if (blockOtherSpell)
            {
                casterRef.playerClasses.isBlockingOtherSpells = false;
            }
            if (impulseSource != null && casterRef.networkObject.IsLocalPlayer) CinemachineImpulseManager.Instance.Clear();
        }



        /// <summary>
        /// Fonctions utilisï¿½es par le serveur pour spawn les gameObjects on start, indï¿½pendamment si collision.
        /// </summary>
        /// 
        public void SpawnObjectsPeriodically()
        {
            if (spawnThisOnStart == null)
                return;
            if (spawnThisOnStart.Count == 0)
                return;

            while (true)
            {
                foreach (var item in spawnThisOnStart)
                {
                    if (item.isNetworkObject)
                        StartCoroutine(SpawnObject(item));
                }
                break;
            }
        }

        private IEnumerator SpawnObject(SpawnType spawnType)
        {
            while (true)
            {
                spawnType.spawnObject = Instantiate(spawnType.spawnObject, gameObject.transform.position, gameObject.transform.rotation);
                spawnType.spawnObject.GetComponent<NetworkObject>().Spawn();
                spawnedObjects.Add(spawnType);

                yield return new WaitForSeconds(spawnType.spawnDelay);
            }
        }


    }

}


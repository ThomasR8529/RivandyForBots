using Cinemachine;
using Lightbug.CharacterControllerPro.Core;
using Lightbug.CharacterControllerPro.Demo;
using Lightbug.CharacterControllerPro.Implementation;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using RealToon.Script;
public class PlayerReference : NetworkBehaviour
{
    public CombatMonster combatMonster;
    public PhysicsMonster physicsMonster;

    [HideInInspector] public PlayerShooting playerShooting;
    [HideInInspector] public PlayerClasses playerClasses;
    public PlayerStatistics playerStatistics;
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector] public PlayerDash playerDash;
    [HideInInspector] public PlayerReincarnation PlayerReincarnation;
    [HideInInspector] public AudioSource audioSource;
    [HideInInspector] public Rigidbody rigidBody;
    [HideInInspector] public NetworkObject networkObject;
    [HideInInspector] public DropSystem dropSystem;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Follow follow;
    [HideInInspector] public AutoAttacks autoAttacks;
    [HideInInspector] public UnityInputHandler unityInputHandler;

    [HideInInspector] public EffectUtils effectUtils;
    public CharacterActor characterActor;
    public Camera3D camera3D;

    public CinemachineVirtualCamera virtualCamera;
    public CharacterBrain characterBrain;

    public CinemachineImpulseSource impulseSource;

    public PlayerAnimations playerAnimations;

    public PlayerDamageUIHook playerDamageHook;

    public Transform headpoint;


    public SmearEffectHelper smearEffectHelper;
    /*    {
            get
            {
                if (characterInputHandler == null)
                {
                    // Tentative de rï¿½cupï¿½ration du composant si _rigidbody est null
                    characterInputHandler = characterBrain.GetComponent<UnityInputHandler>();
                    // Si le composant n'est toujours pas trouvï¿½, ajouter un message d'erreur (facultatif)
                    if (characterInputHandler == null)
                    {
                        Debug.Log("characterInputHandler component not found!");
                    }
                }
                return characterInputHandler;
            }
            set
            {
                characterInputHandler = value;
            }
        }*/

    public UnityEvent OnNetworkSpawnEvent;

    private void Awake()
    {
        playerShooting = GetComponent<PlayerShooting>();
        playerClasses = GetComponent<PlayerClasses>();
        playerStatistics = GetComponent<PlayerStatistics>();
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();
        PlayerReincarnation = GetComponent<PlayerReincarnation>();
        characterActor = GetComponent<CharacterActor>();
        audioSource = GetComponent<AudioSource>();
        rigidBody = GetComponent<Rigidbody>();
        networkObject = GetComponent<NetworkObject>();
        dropSystem = GetComponent<DropSystem>();
        agent = GetComponent<NavMeshAgent>();
        if (playerClasses != null)
        {
            follow = playerClasses.follow;
        }
        effectUtils = GetComponent<EffectUtils>();
        if (effectUtils == null)
        {
            effectUtils = gameObject.AddComponent<EffectUtils>();
        }
    }


    public override void OnNetworkSpawn()
    {
        OnNetworkSpawnEvent?.Invoke();
        if (IsLocalPlayer && IsClient)
        {

            Debug.Log("playerOptimizer disabled");
            DidacticielApi.Instance?.StartIt();
            if (virtualCamera != null)
            {
                virtualCamera.enabled = true;
                virtualCamera.Priority = 10;
                if (virtualCamera.Follow == null)
                {
                    virtualCamera.Follow = camera3D != null ? (Transform)camera3D.transform : transform;
                }
                if (virtualCamera.LookAt == null)
                {
                    virtualCamera.LookAt = virtualCamera.Follow;
                }
            }

        }
        if (!IsLocalPlayer && IsClient)
        {
            if (smearEffectHelper != null) Destroy(smearEffectHelper.gameObject);
            if (characterActor != null) Destroy(characterActor);
            if (camera3D != null) Destroy(camera3D.gameObject);
            if (virtualCamera != null)
            {
                virtualCamera.enabled = false;
                virtualCamera.Priority = 0;
            }
        }
    }

    // Server-authoritative teleport that respects the movement controller when available
    public void ServerTeleport(Vector3 destination)
    {
        if (!IsServer) return;
        try
        {
            if (characterActor != null)
            {
                characterActor.SweepAndTeleport(destination);
                return;
            }
            if (agent != null && agent.enabled)
            {
                agent.Warp(destination);
                return;
            }
            transform.position = destination;
        }
        catch
        {
            transform.position = destination;
        }
    }

    public SmearEffectHelper RecreateSmearEffect(Transform controllerOverride = null)
    {
        if (smearEffectHelper != null)
        {
            Destroy(smearEffectHelper);
            smearEffectHelper = null;
        }

        // smearEffectHelper = gameObject.AddComponent<SmearEffectHelper>();

        // smearEffectHelper.SmearController = controllerOverride != null ? controllerOverride : transform;
        // smearEffectHelper.ApplySettings(5, 100f, 1.5f, 2f, 0f, 5f, 0f);
        // smearEffectHelper.RebuildTargets();
        // smearEffectHelper.DisableSmear();
        return smearEffectHelper;
    }
}



[System.Serializable]
public class PlayerAnimations
{
    public string move;
    public string attack;
    public string spell1;
    public string spell2;
    public string spell3;
    public string spell4;
    public string dead;
    public string jump;
}


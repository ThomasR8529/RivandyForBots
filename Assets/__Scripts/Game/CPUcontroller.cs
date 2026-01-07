using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using Unity.Collections;

public class CPUcontroller : NetworkBehaviour
{


    [SerializeField]
    private Transform spawn1;

    [HideInInspector] public Zone zone;
    private void Awake()
    {
        zone = GetComponent<Zone>();
    }
    void Start()
    {
        Physics.IgnoreLayerCollision(10, 10, true);
#if !UNITY_SERVER
        // Optimisation du jeu:
        Application.targetFrameRate = 60;


#endif

#if UNITY_SERVER
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
#endif
    }


    // Sur � 100%? Attention, c'est une fois que l'on se connecte, cela r�alise cette fonction. MAIS CELA NE CONCERNE QUE NOUS, PAS LES AUTRES !
    void ConnexionClient(ulong ownerId)
    {
        NetworkObject targetPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        Debug.Log(targetPlayer);
        if (targetPlayer.IsLocalPlayer)
        {
            // On le met en localPlayer dans les layers
            targetPlayer.gameObject.layer = 9;
            PlayerData.player.data.clientId = targetPlayer.OwnerClientId;

            // Game Controller:
            GameController.instance.obj = targetPlayer.gameObject;
            GameController.instance.netobj = targetPlayer;
            GameController.instance.LinkEverything();
            GameController.instance.shoot.StartIt();
            ReBinder.Instance.LoadPrefs();
        }

    }



    public void SelectClassInGameSelection()
    {
        GameController.instance.playerReference.playerClasses.SelectClassInGameSelection();
        PlayerChoosedClassServerRPC(GameController.instance.playerReference.gameObject, PlayerData.player.data.accountId, PlayerData.player.data.classId, PlayerData.player.data.reId);
    }


    // [ClientRpc]
    // // Cette fonction permet d'actualiser la classe d'un joueur sp�cifique qu'un joueur a besoin (elle est entre autre
    // // Utilis�e pour le joueur qui vient de se connecter pour r�cup�rer toutes les classes de tous les joueurs
    // public void RefreshDataListClientRpc(NetworkObjectReference playerRef , PlayerStruct playerData, ClientRpcParams clientRpcParams = default)
    // {
    //     if (playerRef.TryGet(out NetworkObject playerNet))
    //     {
    //         Debug.Log("On r�cup�re :" + playerData.playerName.Value + " ayant: " + playerData.classId + "(classId) et " + playerData.reId + "(reId)");
    //         PlayerReference playerReference = playerNet.GetComponent<PlayerReference>();
    //         playerReference.playerClasses.ChangeClass(playerData.classId);
    //         playerReference.playerStatistics.SetPlayerDataLocal(playerData);
    //     }
    // }

    //[ServerRpc(RequireOwnership = false)]
    [ServerRpc(RequireOwnership = false)]
    public void PlayerChoosedClassServerRPC(NetworkObjectReference target, FixedString64Bytes accountId, int classId, int reId, ServerRpcParams serverRpcParams = default)
    {
#if UNITY_SERVER
        if (target.TryGet(out NetworkObject targetObject))
        {
            // Le serveur met � jour localement le playerdata du joueur.
            PlayerReference playerReference = targetObject.GetComponent<PlayerReference>();
            string steamId = "" + accountId;
            PlayerStruct playerData = new PlayerStruct();
            playerData.classId = classId;
            playerData.reId = reId;
            playerReference.playerStatistics.SetPlayerDataLocal(playerData);

            playerReference.playerClasses.ChangeClass(playerData.classId, null, playerData.userId);


            // Position du joueur : � changer en fonction du mode de jeu.
            Vector3 positionAssigned = Run.instance.CPUcontroller.spawn1 == null ? zone.GetAvailableSpawnPoint(true) : Run.instance.CPUcontroller.spawn1.position;

            Debug.Log(positionAssigned);
            playerReference.characterActor.RigidbodyComponent.enabled = false;
            playerReference.characterActor.enabled = false;
            playerReference.rigidBody.position = positionAssigned;
            playerReference.characterActor.RigidbodyComponent.enabled = true;
            playerReference.characterActor.enabled = true;
            playerReference.characterActor.SweepAndTeleport(positionAssigned);
            playerReference.playerMovement.parachute = true;

            //playerReference.characterActor.RigidbodyComponent.SetPositionAndRotation(positionAssigned, new Quaternion());

            // On donne la classe aux autres joueurs
            playerReference.playerStatistics.playerStatData.maxHealth = 250;
            playerReference.playerStatistics.playerStatData.health = 250;
            PlayerChoosedClassClientRPC(positionAssigned, playerData, playerReference.playerStatistics.playerStatData, targetObject);
            playerReference.playerStatistics.playerInstanciated = true;
        }
#endif
    }




    [ClientRpc]
    public void PlayerChoosedClassClientRPC(Vector3 newPosition, PlayerStruct playerData, StatStruct statData, NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObject))
        {
            PlayerReference playerReference = targetObject.GetComponent<PlayerReference>();
            if (targetObject.IsLocalPlayer)
            {
                playerReference.playerStatistics.playerInstanciated = true;
                playerReference.playerStatistics.playerStatData.health = statData.maxHealth;
                playerReference.playerStatistics.baseMaxHealth = statData.maxHealth;
                playerReference.characterActor.RigidbodyComponent.enabled = false;
                playerReference.characterActor.enabled = false;
                playerReference.rigidBody.position = newPosition;
                playerReference.characterActor.RigidbodyComponent.enabled = true;
                playerReference.characterActor.enabled = true;
                playerReference.characterActor.SweepAndTeleport(newPosition);
                playerReference.characterActor.ForceNotGrounded(3);
                // Activation du parachute une fois que le joueur est t�l�port�.
                playerReference.playerMovement.parachute = true;
                //playerReference.characterActor.RigidbodyComponent.SetPositionAndRotation(newPosition, new Quaternion());
                //targetObject.transform.position = newPosition;
            }
            // Je donne la classe du joueur aux autres joueurs.

            playerReference.playerClasses.ChangeClass(playerData.classId, null, playerData.userId);
            // Le joueur met � jour le data de l'autre joueur (celui qui vient de se connecter entre autre).
            playerReference.playerStatistics.SetStatDataLocal(statData);
            playerReference.playerStatistics.SetPlayerDataLocal(playerData);
        }
    }


    [ClientRpc]
    public void UpdateClassClientRPC(StatStruct statData, PlayerStruct playerData, NetworkObjectReference target, bool isReincarnation, ClientRpcParams clientRpcParams = default)
    {

        StartCoroutine(WaitNetObject(statData, playerData, target, isReincarnation));
    }

    public IEnumerator WaitNetObject(StatStruct statData, PlayerStruct playerData, NetworkObjectReference target, bool isReincarnation)
    {
        NetworkObject targetObject = null;

        yield return new WaitUntil(() => target.TryGet(out targetObject));

        if (targetObject != null)
        {
            PlayerReference playerReference = targetObject.GetComponent<PlayerReference>();
            if (!playerReference.networkObject.IsLocalPlayer)
            {
                AudioListener audioListener = playerReference.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    Destroy(audioListener);
                }

            }
            playerReference.playerStatistics.SetPlayerDataLocal(playerData);
            playerReference.playerStatistics.SetStatDataLocal(statData);
            if (isReincarnation)
            {
                playerReference.PlayerReincarnation.Transformation(playerReference.playerStatistics.playerDataGame, playerReference);
            }
            else
            {
                playerReference.playerClasses.ChangeClass(playerData.classId, null, playerData.userId);
            }
        }
        else
        {
            NetworkLog.LogInfoServer("Nous n'avons pas trouv� le gameObject du NetworkObject");
        }
    }

}

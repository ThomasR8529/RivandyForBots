using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class MonsterShower : NetworkBehaviour
{
    [SerializeField] private NetworkObject netObject;

    [SerializeField, HideInInspector] private bool activeMonsterShower;



#if UNITY_SERVER
    public override void OnNetworkSpawn()
    {
        activeMonsterShower = true;
        HideExistingMonstersAndPlayers();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldShowObject(other.gameObject.layer) && activeMonsterShower)
        {
            if (Run.instance.CPUcontroller.zone.hideOtherPlayers && other.gameObject.layer == 3)
            {
                NetworkObject otherNO = other.GetComponent<NetworkObject>();
                if (otherNO == null)
                {
                    HideEntity(other);
                    return;
                }
            }
            PlayerReference otherReference = other.GetComponent<PlayerReference>();
            if (otherReference == null) return;
            if (!otherReference.networkObject.IsSpawned) return;


            if (!otherReference.networkObject.IsNetworkVisibleTo(netObject.OwnerClientId))
            {

                if (otherReference.networkObject.gameObject.layer == 3) otherReference.networkObject.NetworkShow(netObject.OwnerClientId);
                if (otherReference.networkObject.gameObject.layer == 7) otherReference.networkObject.NetworkShow(netObject.OwnerClientId);
                //netObject.NetworkShow(otherNetObject.OwnerClientId);
            }
            if (otherReference.gameObject.layer == 3) StartCoroutine(UpdateTheData(netObject, otherReference.networkObject));
            if (otherReference.gameObject.layer == 7) StartCoroutine(UpdateTheName(netObject, otherReference));

        }
    }

    private IEnumerator UpdateTheName(NetworkObject clientA, PlayerReference clientBRef)
    {
        yield return new WaitUntil(() => clientBRef.networkObject.IsNetworkVisibleTo(clientA.OwnerClientId));
#if UNITY_SERVER
        clientBRef.gameObject.name = clientBRef.follow.twitchName;
        if (clientBRef.follow.billboardEntity == null)
        {
            clientBRef.follow.billboardEntity = clientBRef.GetComponentInChildren<Billboard>(true);
        }
        if (clientBRef.follow.billboardEntity != null)
        {
            clientBRef.follow.billboardEntity.gameObject.SetActive(true);
            clientBRef.follow.billboardEntity.entityName.text = clientBRef.follow.twitchName;
            clientBRef.playerClasses.SetMonsterNameClientRpc(clientBRef.networkObject, clientBRef.follow.twitchName);
        }
#endif

    }

    private IEnumerator UpdateTheData(NetworkObject clientA, NetworkObject clientB)
    {
        yield return new WaitUntil(() => clientA.IsNetworkVisibleTo(clientB.OwnerClientId) && clientB.IsNetworkVisibleTo(clientA.OwnerClientId));

        if (clientB != null && clientB.gameObject.layer == 3)
        {
            // && !netObject.IsNetworkVisibleTo(otherNetObject.OwnerClientId)


            UpdateClassDelayed(clientB, netObject);
            if (clientB.gameObject.layer == 3) UpdateClassDelayed(netObject, clientB);
            // On donne la clase aux autres joueurs
            //GameController.instance.cpu.UpdateClassClientRPC(netObject.GetComponent<PlayerStatistics>().GetPlayerDataLocal(), netObject);
        }
    }

    private void UpdateClassDelayed(NetworkObject otherNetObject, NetworkObject netObject)
    {
        // Attendez que le GameObject soit visible avant d'appeler UpdateClassClientRPC
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { netObject.OwnerClientId }
            }

        };
        PlayerReference playerRef = otherNetObject.GetComponent<PlayerReference>();

        GameController.instance.cpu.UpdateClassClientRPC(playerRef.playerStatistics.playerStatData, playerRef.playerStatistics.playerDataGame, otherNetObject, playerRef.PlayerReincarnation.IsReincarnation, clientRpcParams);
    }


    private void OnTriggerExit(Collider other)
    {
        HideEntity(other);
    }

    private void HideEntity(Collider other)
    {
        if (ShouldShowObject(other.gameObject.layer) && activeMonsterShower)
        {
            NetworkObject otherNetObject = other.GetComponent<NetworkObject>();
            if (otherNetObject == null || !otherNetObject.IsSpawned)
                return;


            if (otherNetObject.IsNetworkVisibleTo(netObject.OwnerClientId))
            {
                if (otherNetObject.gameObject.layer == 3)
                {
                    otherNetObject.NetworkHide(netObject.OwnerClientId);
                }
                if (otherNetObject.gameObject.layer == 7)
                    otherNetObject.NetworkHide(netObject.OwnerClientId);
            }
        }
    }


    private bool ShouldShowObject(int layer)
    {
        // D�finissez ici les couches des monstres et joueurs que vous souhaitez afficher
        int[] visibleLayers = { 3, 7, 9, 10, 12 };

        return visibleLayers.Contains(layer);
    }

    private void HideExistingMonstersAndPlayers()
    {
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList;
        foreach (var spawnedObject in spawnedObjects)
        {
            if (spawnedObject != netObject && spawnedObject.IsPlayerObject)
            {
                // Je cache � tous les spawnedObject le joueur
                spawnedObject.NetworkHide(netObject.OwnerClientId);
            }
        }
        //NetworkObject.NetworkHide(NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Where(obj => obj.gameObject.layer == 3 && obj != netObject).ToList(), netObject.OwnerClientId);
    }
#endif
}

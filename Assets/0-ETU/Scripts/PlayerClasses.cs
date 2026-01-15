using Game;
using Org.BouncyCastle.Bcpg;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using RealToon.Script;

public class PlayerClasses : NetworkBehaviour
{

    public CombatMonster combatMonster;
    public PhysicsMonster physicsMonster;

    public GameObject deathEffect;

    public GameObject warriorMesh;
    public GameObject magicianMesh;
    public GameObject archerMesh;
    public GameObject thiefMesh;
    public GameObject elizaMesh;
    public GameObject serbasMesh;
    public GameObject guldirMesh;
    public GameObject hyugoMesh;
    public GameObject kyrenMesh;

    public GameObject mafaldaMesh;

    private GameObject activeMesh;

    private CharacteDetails detailClass;


    public Animator animator;

    public Follow follow;

    // Niclaus (0) , Lycia (1)
    public Spell[] autoAttacks;
    public Spell[] spells;


    public float warriorSpell1CD;
    public float warriorSpell2CD;
    public float warriorSpell3CD;
    public float warriorSpell4CD;

    public float spiritSpell1CD;
    public float spiritSpell2CD;
    public float spiritSpell3CD;
    public float spiritSpell4CD;

    // Ici les sorts crï¿½ent ig (pour pouvoir les cancel entre autres)
    public Spell[] humanRealSpells = new Spell[4];
    public Spell[] reincarnationRealSpells = new Spell[4];
    public bool[] cancelSpells = new bool[4];

    [Header("Monster")]
    public bool isMonster = false;

    [HideInInspector] public bool isBlockingOtherSpells = false;

    private PlayerShooting shooter;
    private PlayerReference playerReference;

    public void SelectClassInGameSelection()
    {
        if (gameObject.layer == 12) return;
        shooter = GetComponent<PlayerShooting>();
        playerReference = GetComponent<PlayerReference>();

        // S'il ne s'agit pas d'un monstre
        if (IsLocalPlayer && !isMonster)
        {
            ChangeClass(PlayerData.player.data.classId);
        }
        if (isMonster)
        {
            follow = GetComponent<Follow>();
        }
    }

    public void ChangeClass(int classId, GameObject reincarnationObj = null, int? targetUserId = null)
    {
        if (playerReference == null)
        {
            playerReference = GetComponent<PlayerReference>();
        }
        animator = classId switch
        {
            9 => mafaldaMesh.GetComponent<Animator>(),
            8 => kyrenMesh.GetComponent<Animator>(),
            7 => hyugoMesh.GetComponent<Animator>(),
            6 => guldirMesh.GetComponent<Animator>(),
            5 => serbasMesh.GetComponent<Animator>(),
            4 => elizaMesh.GetComponent<Animator>(),
            3 => thiefMesh.GetComponent<Animator>(),
            2 => archerMesh.GetComponent<Animator>(),
            1 => magicianMesh.GetComponent<Animator>(),
            0 => warriorMesh.GetComponent<Animator>(),
            10 => reincarnationObj.GetComponent<Animator>(),
            _ => hyugoMesh.GetComponent<Animator>(),
        };

        switch (classId)
        {
            case 10:
                if (reincarnationObj != null)
                {
                    detailClass = reincarnationObj.GetComponent<CharacteDetails>();
                }
                break;
            case 0:
                detailClass = warriorMesh.GetComponent<CharacteDetails>();
                break;
            case 1:
                detailClass = magicianMesh.GetComponent<CharacteDetails>();
                break;
            case 2:
                detailClass = archerMesh.GetComponent<CharacteDetails>();
                break;
            case 3:
                detailClass = thiefMesh.GetComponent<CharacteDetails>();
                break;
            case 4:
                detailClass = elizaMesh.GetComponent<CharacteDetails>();
                break;
            case 5:
                detailClass = serbasMesh.GetComponent<CharacteDetails>();
                break;
            case 6:
                detailClass = guldirMesh.GetComponent<CharacteDetails>();
                break;
            case 7:
                detailClass = hyugoMesh.GetComponent<CharacteDetails>();
                break;
            case 8:
                detailClass = kyrenMesh.GetComponent<CharacteDetails>();
                break;
            case 9:
                detailClass = mafaldaMesh.GetComponent<CharacteDetails>();
                break;
        }

        autoAttacks = detailClass.autoAttacks;
        spells = detailClass.spells;

#if !UNITY_SERVER
        MeshActivation(classId, reincarnationObj);
        Debug.Log("RequestEquippedItemsServerRpc : " + targetUserId);
        RequestEquippedItemsServerRpc(targetUserId.HasValue ? (int)targetUserId : PlayerData.player.data.userId, classId);
#else
        MeshActivation(classId, reincarnationObj);
#endif


        if (IsLocalPlayer)
        {
            cooldownUI.instance.UpdateIt();
            UpdatePlayerSpellServerRpc(gameObject.GetComponent<NetworkObject>());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEquippedItemsServerRpc(int userId, int classId, ServerRpcParams serverRpcParams = default)
    {
#if UNITY_SERVER
        Debug.Log(userId + " et " + classId);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
            }
        };

        // Convertir les donnÃ©es en un format transmissible sur le rÃ©seau
        List<ShopUserItemData> serializedItems = new List<ShopUserItemData>();


        SendEquippedItemsClientRpc(classId, serializedItems.ToArray(), clientRpcParams);
#endif
    }

    [ClientRpc]
    private void SendEquippedItemsClientRpc(int classId, ShopUserItemData[] equippedItems, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(gameObject.name + " et " + classId);
        foreach (var item in equippedItems)
        {
            if (item.Category == 0)
            {
                Debug.Log("SelectSkin with " + item.ShopId);
                detailClass.characterSkin.SelectSkin(item.ShopId);
            }
            MeshActivation(classId);
        }
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
        }
        else
        {
            Debug.LogWarning("Le NetworkObjectReference du monstre n'a pas pu Ãªtre rÃ©solu.");
        }
    }



    [ServerRpc]
    private void UpdatePlayerSpellServerRpc(NetworkObjectReference casterObject)
    {
#if UNITY_SERVER
        if (casterObject.TryGet(out NetworkObject casterNet))
        {
            if (detailClass == null) return;
            PlayerClasses casterClass = casterNet.GetComponent<PlayerClasses>();
            casterClass.autoAttacks = detailClass.autoAttacks;
            casterClass.spells = detailClass.spells;
            UpdatePlayerSpellClientRpc(casterObject);
        }
        // On ne le fait que cï¿½tï¿½ client (owner) et serveur, pas besoin de le faire pour le moment pour les autres clients.
        // Peut ï¿½tre un jour ? mais pour le moment, pas nï¿½cessaire.
#endif
    }

    [ClientRpc]
    private void UpdatePlayerSpellClientRpc(NetworkObjectReference casterObject)
    {
        if (casterObject.TryGet(out NetworkObject casterNet))
        {
            if (casterNet.IsOwner) return;
            PlayerClasses casterClass = casterNet.GetComponent<PlayerClasses>();
            if (casterClass.detailClass == null)
                return;
            casterClass.autoAttacks = casterClass.detailClass.autoAttacks;
            casterClass.spells = casterClass.detailClass.spells;
        }
    }

    public void MeshActivation(int choosedClassId, GameObject customMesh = null)
    {
        warriorMesh.SetActive(choosedClassId == 0 ? true : false);
        magicianMesh.SetActive(choosedClassId == 1 ? true : false);
        archerMesh.SetActive(choosedClassId == 2 ? true : false);
        thiefMesh.SetActive(choosedClassId == 3 ? true : false);
        elizaMesh.SetActive(choosedClassId == 4 ? true : false);
        serbasMesh.SetActive(choosedClassId == 5 ? true : false);
        guldirMesh.SetActive(choosedClassId == 6 ? true : false);
        hyugoMesh.SetActive(choosedClassId == 7 ? true : false);
        kyrenMesh.SetActive(choosedClassId == 8 ? true : false);
        if (mafaldaMesh != null)
            mafaldaMesh.SetActive(choosedClassId == 9 ? true : false);

        GameObject newActiveMesh = null;

        switch (choosedClassId)
        {
            case 0: newActiveMesh = warriorMesh; break;
            case 1: newActiveMesh = magicianMesh; break;
            case 2: newActiveMesh = archerMesh; break;
            case 3: newActiveMesh = thiefMesh; break;
            case 4: newActiveMesh = elizaMesh; break;
            case 5: newActiveMesh = serbasMesh; break;
            case 6: newActiveMesh = guldirMesh; break;
            case 7: newActiveMesh = hyugoMesh; break;
            case 8: newActiveMesh = kyrenMesh; break;
            case 9: newActiveMesh = mafaldaMesh; break;
            case 10: newActiveMesh = customMesh; break;
        }

        activeMesh = newActiveMesh;

        // Rafraichit les cibles du smear après un swap de mesh (transform/reincarnation).
        if (playerReference == null)
        {
            playerReference = GetComponent<PlayerReference>();
        }

        if (playerReference != null)
        {
            playerReference.RecreateSmearEffect(newActiveMesh != null ? newActiveMesh.transform : transform);
        }
    }

    /// <summary>
    /// Enable or disable all renderers on the current player mesh (human or reincarnation).
    /// </summary>
    public void SetCharacterMeshVisible(bool visible)
    {
        GameObject targetMesh = activeMesh;

        if (targetMesh == null)
        {
            Debug.LogWarning("SetCharacterMeshVisible called without an active mesh.");
            return;
        }

        foreach (var renderer in targetMesh.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }


    public void CastAutoAttack(int index)
    {
        if (shooter != null)
        {
            shooter.NotifyAutoAttackDuringCast();
        }
        shooter.LaunchAutoAttack(autoAttacks[index], index);
    }

}

// Classe pour transmettre les donnÃ©es sur le rÃ©seau
[System.Serializable]
public struct ShopUserItemData : INetworkSerializable
{
    public int ShopId;

    public int Category;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ShopId);
        serializer.SerializeValue(ref Category);
    }
}






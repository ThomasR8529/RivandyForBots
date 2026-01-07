using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
public class selectUI : NetworkBehaviour
{
/*    public GameObject SelectionPanel;

    public GameObject warriorSkin;
    public GameObject magicianSkin;
    public GameObject archerSkin;
    public GameObject thiefSkin;

    public GameObject SelectionUI;
    public GameObject mainCamera;

    private int choosedClass;

    public GameObject selectButton;
    public GameObject selectButtonText;
    public GameObject warriorSelectionUI;
    public GameObject magicianSelectionUI;
    public GameObject archerSelectionUI;
    public GameObject thiefSelectionUI;

    private ClassUIicon warriorClassUI;
    private ClassUIicon magicianClassUI;
    private ClassUIicon archerClassUI;
    private ClassUIicon thiefClassUI;


    private GameObject ownerPlayer;
    private GameObject playerAffected;

    public GameObject gameUi;*/

/*    public RawImage Qspell;
    public RawImage Zspell;
    public RawImage Espell;
    public RawImage Rspell;
    
*//*    public TextMeshProUGUI characterName;*//*

    public void ChangeIconClass(GameObject player)
    {
        PlayerClasses classPlayer = player.GetComponent<PlayerClasses>();
        Qspell.texture = classPlayer.spells[0].avatar;
        Zspell.texture = classPlayer.spells[1].avatar;
        Espell.texture = classPlayer.spells[2].avatar;
        Rspell.texture = classPlayer.spells[3].avatar;
        Cursor.lockState = CursorLockMode.Locked;
    }*/
/*
    public void selectWarrior()
    {
        picked(0);
        characterName.text = "Niclaus";
    }

    public void selectMagician()
    {
        picked(1);
        characterName.text = "Lycia";
    }

    public void selectArcher()
    {
        picked(2);
        characterName.text = "Kimka";
    }

    public void selectThief()
    {
        picked(3);
        characterName.text = "Kemo";
    }

    public void confirmSelection()
    {
      
        selectButton.GetComponent<Button>().interactable = false;
        selectButtonText.GetComponent<Text>().text = "Waiting for players...";

        // Le joueur a choisi sa classe donc on l'envoit au serveur.
        PlayerChoosedClassServerRPC(choosedClass, NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject);
        SelectionPanel.SetActive(false);
        SelectionUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        
        // On active l'interface de jeu
        gameUi.SetActive(true);

        *//*        ownerPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
                ownerPlayer.SetActive(true);*//*
    }

    // Fonction permettant de n'afficher uniquement la classe sélectionnée.
    void picked(int number)
    {
        warriorClassUI.selectCharacter(number == 0 ? true : false);
        magicianClassUI.selectCharacter(number == 1 ? true : false);
        archerClassUI.selectCharacter(number == 2 ? true : false);
        thiefClassUI.selectCharacter(number == 3 ? true : false);
        choosedClass = number;
        warriorSkin.SetActive(number == 0 ? true : false);
        magicianSkin.SetActive(number == 1 ? true : false);
        archerSkin.SetActive(number == 2 ? true : false);
        thiefSkin.SetActive(number == 3 ? true : false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerChoosedClassServerRPC(int choosedClass, NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObject))
        {
            PlayerClasses classPlayer = targetObject.gameObject.GetComponent<PlayerClasses>();
            // On active uniquement la classe sélectionnée.
            classPlayer.SetPlayerClass(choosedClass);
            classPlayer.warriorMesh.SetActive(choosedClass == 0 ? true : false);
            classPlayer.magicianMesh.SetActive(choosedClass == 1 ? true : false);
            classPlayer.archerMesh.SetActive(choosedClass == 2 ? true : false);
            classPlayer.thiefMesh.SetActive(choosedClass == 3 ? true : false);
            classPlayer.animator = choosedClass == 0 ? classPlayer.warriorMesh.GetComponent<Animator>() : choosedClass == 1 ? classPlayer.magicianMesh.GetComponent<Animator>() : choosedClass == 2 ? classPlayer.archerMesh.GetComponent<Animator>() : classPlayer.thiefMesh.GetComponent<Animator>();
            classPlayer.SetPlayerClass(choosedClass);
            Debug.Log("Changement de l'animator pour le joueur: " + targetObject.gameObject.name);

            Vector3 positionAssigned = SelectionTimer.GetRandomPositionOnPlane();
            Debug.Log(targetObject.name + " a rejoint la partie en position: " + positionAssigned);
            targetObject.transform.position = positionAssigned;

            PlayerChoosedClassClientRPC(positionAssigned, choosedClass, targetObject.gameObject);
        }
    }
    [ClientRpc]
    private void PlayerChoosedClassClientRPC(Vector3 newPosition, int choosedClass, NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObject))
        {
            if (IsClient)
            {
                PlayerClasses classPlayer = targetObject.gameObject.GetComponent<PlayerClasses>();
                classPlayer.SetPlayerClass(choosedClass);
                classPlayer.warriorMesh.SetActive(choosedClass == 0 ? true : false);
                classPlayer.magicianMesh.SetActive(choosedClass == 1 ? true : false);
                classPlayer.archerMesh.SetActive(choosedClass == 2 ? true : false);
                classPlayer.thiefMesh.SetActive(choosedClass == 3 ? true : false);
                classPlayer.animator = choosedClass == 0 ? classPlayer.warriorMesh.GetComponent<Animator>() : choosedClass == 1 ? classPlayer.magicianMesh.GetComponent<Animator>() : choosedClass == 2 ? classPlayer.archerMesh.GetComponent<Animator>() : classPlayer.thiefMesh.GetComponent<Animator>();
                classPlayer.SetPlayerClass(choosedClass);
                if (targetObject.IsLocalPlayer)
                {
                    // playerShoot.AdjustShootRate();
                    targetObject.transform.position = newPosition;
                    // On modifie l'interface des sorts:
                    Qspell.texture = classPlayer.spells[0].avatar;
                    Zspell.texture = classPlayer.spells[1].avatar;
                    Espell.texture = classPlayer.spells[2].avatar;
                    Rspell.texture = classPlayer.spells[3].avatar;

                }
            }
        }
        // playerMove.animator = selectAnimator;
    }*/

    // S'il n'a pas pick: NetworkManager.DisconnectClient();


}


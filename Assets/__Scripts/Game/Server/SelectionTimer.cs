using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectionTimer : NetworkBehaviour

{
    public NetworkVariable<float> selectTime = new NetworkVariable<float>(100f);
    // float timeRemaining = 240;
    float timeRemaining = 75;
    bool timerIsRunning = false;
    public TextMeshProUGUI selectTextCounter;

    Animator animator;
    GameObject player;

/*    [SerializeField]
    private Transform spawn1;
    [SerializeField]
    private Transform spawn2;
    private void Start()
    {
        if (IsServer)
        {
            timerIsRunning = true;
        }

        selectTime.OnValueChanged += OnSelectTimeChanged;
        OnSelectTimeChanged(selectTime.Value, selectTime.Value);
    }

    void Update()
    {
        if (IsServer)
        {
            if (timerIsRunning)
            {
                if (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    selectTime.Value = timeRemaining;
                    // DisplayTime(timeRemaining);
                }
                else
                {
                    Debug.Log("Champion Selection ended.");
                    Debug.Log("Time to start the game !");
                    timeRemaining = 0;
                    selectTime.OnValueChanged -= OnSelectTimeChanged;
                    // CallChampionSelectionEndedClientRPC();
                    timerIsRunning = false;
                    // enabled = false;
                }
            }
        }
        else
        {
            if (!player && NetworkManager.IsConnectedClient)
            {
                player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
                animator = player.GetComponent<Animator>();
            }
        }

    }

    public void OnSelectTimeChanged(float previous, float newValue)
    {
        float minutes = Mathf.FloorToInt(newValue / 60);
        float seconds = Mathf.FloorToInt(newValue % 60);
        if (IsClient)
        {
            if (newValue > 1)
            {
                selectTextCounter.SetText(minutes.ToString() + " m " + seconds.ToString() + "s left");
            }
            else
            {
                selectTextCounter.SetText(minutes.ToString() + "Waiting for players...");
                selectTextCounter.gameObject.SetActive(false);
            }

            // Modifier ses vie dans l'UI:
            // GameObject.Find("HealthGameUi").GetComponent<Text>().text = newValue.ToString() + "/ 100";

        }
        // healthBar.GetComponent<Slider>().value = newValue;
        // Todo: Plus tard, faire en sorte d'afficher une UI qui montre que le joueur perd des vies.

    }*/

/*    [ServerRpc(RequireOwnership = false)]
    private void CallDisconnectMeServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong playerid = NetworkManager.SpawnManager.GetPlayerNetworkObject(serverRpcParams.Receive.SenderClientId).OwnerClientId;
        // Déporter le disconnect client ici vus que c'est au serveur de disconnecter les joueurs.
        // Possible que ce ne soit pas cet id qu'il faut utiliser, vérifier que ça déconnecte le bon joueur à la fin de la sélection de perso.
        NetworkManager.DisconnectClient(playerid);
    }*/

/*    [ServerRpc(RequireOwnership = false)]
    private void CallSpawnMeNowServerRpc(NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject targetObject))
        {
            Vector3 definedPosition = Vector3.zero;
            if (SceneManager.GetActiveScene().name == "Level1")
            {
                definedPosition = GetRandomPositionOnPlane();
            }
            else
            {
                Debug.Log("Définition du Spawn du joueur avec le gameobject");
                definedPosition = spawn1.position;
            }

            // mouvementJoueur.gravity = -1;
            targetObject.transform.position = definedPosition;
            // mouvementJoueur.firstGrounded = false;
            Debug.Log("Demande du changement de position du joueur :" + targetObject.name);
            CallSpawnMeNowClientRpc(definedPosition, targetObject);
        }

    }*/

/*    [ClientRpc]
    private void CallSpawnMeNowClientRpc(Vector3 newPosition, NetworkObjectReference target)
    {
        // if (IsLocalPlayer) return;
        // Je m'arrêtais là 
        if (target.TryGet(out NetworkObject targetObject))
        {
            // PlayerMovement mouvementJoueur = targetObject.GetComponent<PlayerMovement>();
            // mouvementJoueur.gravity = -1;
            targetObject.transform.position = newPosition;
            Debug.Log("définition de la position de : " + targetObject.name + " en local.");
            // mouvementJoueur.firstGrounded = false;
            // animator.SetBool("isDiving", true);
        }
    }*/

/*    public static Vector3 GetRandomPositionOnPlane()
    {
        return Zone.GetRandomPositionDependingOnZone(0, true);
    }*/
}

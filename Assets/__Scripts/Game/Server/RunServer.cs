using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

using System;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
public class RunServer : MonoBehaviour
{
    // IDs utilisés par le serveur
    public static int serverId;
    public static int modeId;

    // Pour l'Inspector (le "vrai" paramétrage avant build)
    public int serverToPassId;
    public int modeToPassId;

    // Instance statique pour pouvoir appeler RunServer.instance.LoadNewMap() depuis d'autres scripts
    public static RunServer instance;

    [SerializeField] private Zone zone;

#if UNITY_SERVER
    // Pour éviter de lancer plusieurs chargements de map en parallèle
    private bool isLoadingMap = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Méthode publique pour charger une nouvelle map :
    ///   - Arrêter l'ancien Netcode si besoin,
    ///   - Charger un level aléatoire,
    ///   - Mettre à jour le status BDD,
    ///   - Relancer ou continuer le serveur Netcode.
    /// </summary>
    public void LoadNewMap()
    {
        if (isLoadingMap)
        {
            Debug.LogWarning("[RunServer] Un chargement de map est déjà en cours...");
            return;
        }

        LoadMapCoroutine();
    }

    private void LoadMapCoroutine()
    {
        isLoadingMap = true;

        Debug.Log("[RunServer] Début du chargement d'une nouvelle map (BDD).");

        string randomLevel = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(randomLevel))
        {
            randomLevel = "Survivor";
        }
        Debug.Log($"[RunServer] Map sélectionnée : {randomLevel} (modeId={modeId}).");


        SceneManager.LoadSceneAsync(randomLevel, LoadSceneMode.Single);

    }

#endif

}

[Serializable]
public class ServerConfig
{
    public int serverId;
    public int modeId;
    public string address;
    public string port;
}

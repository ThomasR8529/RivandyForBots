using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    public static void PrepareScene(string sceneName) {
        // Vérifier si la scène existe
        if (SceneManager.GetSceneByName(sceneName) != null) {
            AsyncOperation sceneLoadingOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            sceneLoadingOperation.allowSceneActivation = false; // Empêcher le chargement automatique


        }
        else {
            Debug.LogError("La scène demandée n'existe pas.");
        }
    }

    public static void StartScene(string sceneName) {
        if (!string.IsNullOrEmpty(sceneName)) {
            if (SceneManager.GetSceneByName(sceneName) != null) {

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            }
            else {
                Debug.LogError("La scène demandée n'existe pas.");
            }
        }
        else {
            Debug.LogError("Aucune scène n'a été spécifiée pour le démarrage.");
        }
    }
}
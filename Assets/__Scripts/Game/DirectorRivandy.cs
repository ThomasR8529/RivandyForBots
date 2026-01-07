using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DirectorRivandy : MonoBehaviour
{
    [System.Serializable]
    public class DirectorScenePair
    {
        public PlayableDirector director;
        public GameObject sceneObject;
    }

    public List<DirectorScenePair> directorScenePairs = new List<DirectorScenePair>(); // Liste des paires Director/Scene
    private int currentDirectorIndex = 0; // Index du DirectorScenePair actif

    void Start()
    {
        // Désactiver tous les GameObjects sauf celui du premier DirectorScenePair
        for (int i = 0; i < directorScenePairs.Count; i++)
        {
            bool isActive = i == currentDirectorIndex;
            directorScenePairs[i].sceneObject.SetActive(isActive);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetCurrentTimeline();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GoToPreviousDirector();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            GoToNextDirector();
        }
    }

    private void ResetCurrentTimeline()
    {
        PlayableDirector currentDirector = directorScenePairs[currentDirectorIndex].director;
        currentDirector.time = 0;
        currentDirector.Evaluate(); // Réinitialiser immédiatement l'état visuel de la timeline
    }

    private void GoToPreviousDirector()
    {
        if (currentDirectorIndex > 0)
        {
            SwitchDirector(currentDirectorIndex - 1);
        }
    }

    private void GoToNextDirector()
    {
        if (currentDirectorIndex < directorScenePairs.Count - 1)
        {
            SwitchDirector(currentDirectorIndex + 1);
        }
    }

    private void SwitchDirector(int newDirectorIndex)
    {
        if (newDirectorIndex < 0 || newDirectorIndex >= directorScenePairs.Count)
        {
            return; // Index invalide
        }

        // Désactiver le GameObject de la scène actuelle
        directorScenePairs[currentDirectorIndex].sceneObject.SetActive(false);

        // Activer le GameObject de la nouvelle scène
        currentDirectorIndex = newDirectorIndex;
        directorScenePairs[currentDirectorIndex].sceneObject.SetActive(true);
    }
}

using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class RandomAnimationPlayer : MonoBehaviour
{
    private PlayableDirector playableDirector;

    [SerializeField]
    private List<PlayableAsset> animations;

    void Awake()
    {
        // Récupère le PlayableDirector attaché à l'objet
        playableDirector = GetComponent<PlayableDirector>();
    }

    public void PlayRandomAnimation()
    {
        if (animations == null || animations.Count == 0)
        {
            Debug.LogWarning("Aucune animation assignée dans la liste.");
            return;
        }

        // Sélectionne un PlayableAsset aléatoirement
        int randomIndex = Random.Range(0, animations.Count);
        PlayableAsset selectedAnimation = animations[randomIndex];

        // Assigne et joue l'animation
        playableDirector.playableAsset = selectedAnimation;
        playableDirector.Play();
    }
} 

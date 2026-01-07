using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

public class DidacticielManager : NetworkBehaviour
{
    public static DidacticielManager instance;

    [SerializeField] PlayableDirector playableDirector;

    [SerializeField] PlayableAsset[] steps;

    int index = 0;

    private void Awake() {
        if(instance == null) {
            instance = this;
        }
    }

	public void StartIt() {
        if (IsClient) {
            GameController.instance.playerReference.playerMovement.enabled = false;
            GameController.instance.playerReference.playerShooting.enabled = false;
            GameController.instance.playerReference.playerDash.enabled = false;
            GameController.instance.playerReference.PlayerReincarnation.enabled = false;

            playableDirector.playableAsset = steps[0];
            playableDirector.Play();
        }

	}

    public void PassNextTimeLine() {
        if (IsClient) {
            index++;
            playableDirector.Stop();
            playableDirector.playableAsset = steps[index];
            playableDirector.Play();
        }
    }

    public void ActivateMovement() {
        GameController.instance.playerReference.playerMovement.enabled = true;
    }

    public void ActivateSpellsAndAuto() {
            GameController.instance.playerReference.playerShooting.enabled = true;
    }
    public void ActivateDash() {
            GameController.instance.playerReference.playerDash.enabled = true;
    }

    public void ActivateReincarnation() {
            GameController.instance.playerReference.PlayerReincarnation.enabled = true;
    }

    public void ChangeScene() {
        Cursor.lockState = CursorLockMode.None;
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        StartCoroutine(Loading.instance.LoadSceneAsync("Home"));
    }

}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public string nextSceneName;

    private VideoPlayer videoPlayer;
    private bool hasSceneLoaded = false;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.loopPointReached += EndReached;
        videoPlayer.Prepare();
    }

    void Update()
    {
        if (!hasSceneLoaded && Input.anyKeyDown)
        {
            Debug.Log("User pressed a key. Loading next scene...");
            LoadNextScene();
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video is prepared and ready to play.");
        videoPlayer.Play();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Error loading video: " + message);
        LoadNextScene();
    }

    void EndReached(VideoPlayer vp)
    {
        Debug.Log("Video ended. Loading next scene...");
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (!hasSceneLoaded)
        {
            hasSceneLoaded = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
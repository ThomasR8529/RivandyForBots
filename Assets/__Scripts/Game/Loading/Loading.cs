using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{

    public static Loading instance;

    [SerializeField] private GameObject loadingContainer;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI loadingText;

    [SerializeField] private LocalizedText loadingMapName;
    [SerializeField] private Image loadingIcon;

    [SerializeField] private Image backgroundImg;

    [SerializeField] private Sprite survivorSprite;
    [SerializeField] private Sprite battleRoyaleSprite;

    [SerializeField] private Sprite streamerSprite;

    private void Awake()
    {
        if (instance == null) instance = this;
        if (loadingContainer.activeSelf) loadingContainer.SetActive(false);
    }


    public IEnumerator LoadSceneAsync(string mapName, int serverMode = -1)
    {
        if (serverMode == (int)GameMode.Survivor)
        {
            backgroundImg.sprite = survivorSprite;
            loadingMapName.SetText(10);
        }
        else if (serverMode == (int)GameMode.BattleRoyale)
        {
            backgroundImg.sprite = battleRoyaleSprite;
            loadingMapName.SetText(9);
        }
        else if (serverMode == (int)GameMode.Streamer)
        {
            backgroundImg.sprite = streamerSprite;
            loadingMapName.SetText(111);
        }
        loadingContainer.SetActive(true);
        slider.value = 0;

        AsyncOperation op = SceneManager.LoadSceneAsync(mapName);
        // op.allowSceneActivation = true;
        while (!op.isDone)
        {
            slider.value = op.progress;
            loadingText.text = $"{op.progress * 100f:F0}%";

            yield return null;
        }


    }
}

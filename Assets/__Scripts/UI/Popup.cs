using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI text;

    [SerializeField]
    private Slider slider;


    [SerializeField]
    private float timeLength = 5.0f;


    private void Start()
    {
        StartCoroutine(DestroyAuto());
    }

    public void SetText(string content)
    {
        text.text = content;
    }

    public void SetTimeLength(float time)
    {
        timeLength = time;
    }

    IEnumerator DestroyAuto()
    {
        float time = 0.0f;
        while (time < timeLength)
        {
            slider.value = time * 1.0f / timeLength;
            time += Time.deltaTime;
            yield return null;
        }
        DestroyPopup();
    }
    public void DestroyPopup()
    {
        Destroy(gameObject);
    }
}

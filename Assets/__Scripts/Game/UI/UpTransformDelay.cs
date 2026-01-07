using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpTransformDelay : MonoBehaviour
{
    float startedTime;

    void Start()
    {
        startedTime = Time.time;
    }

    // Ce script permet entre autre de monter l'UI des vies vers le haut
    void Update()
    {
        StartCoroutine(MoveUp());
    }

    IEnumerator MoveUp()
    {
        while (Time.time < startedTime + 2.0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.003f, transform.position.z);
            yield return new WaitForSeconds(0.05f);
        }

    }
}

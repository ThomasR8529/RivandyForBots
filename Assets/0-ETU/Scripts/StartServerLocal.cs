using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StartServerLocal : MonoBehaviour
{
    [SerializeField] float secondBeforeStart;
    void Start()
    {
        StartCoroutine(startServerInSeconds(secondBeforeStart));
    }

    private IEnumerator startServerInSeconds(float secondBeforeStart)
    {
        yield return new WaitForSeconds(secondBeforeStart);
        NetworkManager.Singleton.StartHost();
    }
}

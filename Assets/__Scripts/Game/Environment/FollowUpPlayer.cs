using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class FollowUpPlayer : MonoBehaviour
{
    private GameObject player;

#if !UNITY_SERVER


    void Update()
    {
        if (player)
        {
            transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 13.0f, player.transform.position.z);
        }
        else
        {
            player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
        }
    }
#endif
}


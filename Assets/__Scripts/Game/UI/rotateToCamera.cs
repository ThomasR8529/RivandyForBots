using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class rotateToCamera : MonoBehaviour
{
    // Start is called before the first frame update
    // Update is called once per frame
    Transform localPlayer;

#if !UNITY_SERVER
    void Update()
    {
        if (localPlayer == null && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
        {
            localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerReference>().camera3D.transform;
        }
        if(localPlayer != null)
        {
            transform.LookAt(localPlayer.transform);
        }
    }

#endif
}

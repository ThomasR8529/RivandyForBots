using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TriggerBackLobby : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            if (other.gameObject.layer == 3)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { other.gameObject.GetComponent<NetworkObject>().NetworkObjectId }
                    }

                };
            }
        }
    }
}

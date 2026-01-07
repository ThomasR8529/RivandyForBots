using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class DropSystem : NetworkBehaviour
{

    [SerializeField] private bool dropEverything;
    [SerializeField] private List<GameObject> dropList;
    [SerializeField] private int minDropNbr = 0;
    [SerializeField] private int maxDropNbr = 3;
    [SerializeField, HideInInspector] PlayerReference playerReference;
    [SerializeField, Header("Push Item speed (default = 10)")]float speed = 10;

    void Start()
    {
        playerReference = GetComponent<PlayerReference>();
    }

    public void DropItNow(PlayerReference deserveFor)
    {
        if (!IsServer) return;
        if (dropList == null || dropList.Count == 0) return;

        var spawned = NetworkObject.Instantiate(dropList[0], transform.position, default);
        var netObj = spawned.GetComponent<NetworkObject>();
        if (netObj == null) return;

        var interactionItem = spawned.GetComponent<InteractionItem>();
        if (interactionItem != null)
        {
            interactionItem.AssignDeserveFor(deserveFor);
        }

        netObj.Spawn();
    }
}

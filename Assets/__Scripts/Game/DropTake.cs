using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropTake : MonoBehaviour
{

    [SerializeField, Header("ID du DROP: 0 = Soul")]
    private int dropId;

    [SerializeField, Header("Nombre de fois que le drop est attribu�")]
    private float amount;


    [SerializeField, Header("Effet instanci� qui va �tre jou� quand un drop est r�cup�r�")] private GameObject dropTakenImpactEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 || other.gameObject.layer == 9)
        {


#if UNITY_SERVER
            PlayerStatistics playerStatistics = other.GetComponent<PlayerReference>().playerStatistics;
            playerStatistics.UpdateStatDataClientRpc(playerStatistics.playerStatData);

            playerStatistics.playerStatData.soul += 1;
#else
            Instantiate(dropTakenImpactEffect, gameObject.transform.position, gameObject.transform.rotation);
            Destroy(gameObject);
#endif
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Buff : MonoBehaviour
{
    [SerializeField]
    bool isBuff = true;
    [SerializeField]
    bool keepPlayerPosition = true;
    [SerializeField]
    bool keepPlayerRotation;
    [SerializeField]
    float buffSeconds;

    [SerializeField]
    float freezeEntitySeconds;

    [SerializeField]
    int shieldAmount;
    [SerializeField]
    float shieldDuration;
    [SerializeField]
    List<float> shieldListValue;


GameObject player;

    PlayerReference playerReference;


    public void SetCaster(GameObject objecto)
    {
        if (objecto != null && player == null)
        {
            player = objecto;
            playerReference = objecto.GetComponent<PlayerReference>();
            shieldListValue = new List<float>() { shieldAmount, shieldDuration, Time.time, 0 };
            StartCoroutine(DestroyBuff());
        }
    }

    void Update()
    {
        if (keepPlayerPosition && player != null)
        {
            transform.position = player.transform.position;
        }
        if (keepPlayerRotation && player != null)
        {
            transform.rotation = player.transform.rotation;
        }

    }

    public IEnumerator DestroyBuff()
    {
        if (freezeEntitySeconds > 0) playerReference.playerStatistics.FreezeSeconds = freezeEntitySeconds;
        if (shieldAmount > 0 && shieldDuration > 0)
            Debug.Log("On ajoute � la shieldList");
        if (playerReference.playerStatistics.shieldBar != null && shieldAmount > 0 && shieldDuration > 0) playerReference.playerStatistics.shieldBar.gameObject.SetActive(true);
        if (shieldAmount > 0 && shieldDuration > 0) playerReference.playerStatistics.shieldList.Add(shieldListValue);


        yield return new WaitForSeconds(buffSeconds);
        if(playerReference.playerStatistics.shieldBar != null) playerReference.playerStatistics.shieldBar.gameObject.SetActive(false);
        playerReference.playerStatistics.shieldList.Remove(shieldListValue);
        Destroy(gameObject);
    }
}
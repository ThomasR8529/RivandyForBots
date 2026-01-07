using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupTrigger : MonoBehaviour
{
    [SerializeField]
    private Transform parentLayout;

    [SerializeField]
    private GameObject popupPrefab;

    [SerializeField]
    private string content;

    [SerializeField]
    private float timeLength;

    [SerializeField]
    private bool onceTime = true;

    [SerializeField]
    private int count = 0;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 3 || other.gameObject.layer == 9)
        {
            Debug.Log("J'entre dans le TriggerPopup");
            if (onceTime && count == 0)
            {
                Debug.Log("TRIGGER POPUP SPAWNING");
                count++;
                GameObject popupSpawned = Instantiate(popupPrefab, parentLayout);
                popupSpawned.GetComponent<Popup>().SetText(content);
                popupSpawned.GetComponent<Popup>().SetTimeLength(timeLength);
            }
        }
    }
}

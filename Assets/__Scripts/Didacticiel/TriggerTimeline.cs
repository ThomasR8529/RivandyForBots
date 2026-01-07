using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerTimeline : MonoBehaviour
{

    [SerializeField] UnityEvent eventOnTriggerEnter;



#if !UNITY_SERVER
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.layer == 3 || other.gameObject.layer == 9) {
            eventOnTriggerEnter?.Invoke();
            Destroy(this);
        }
    }
#endif
}

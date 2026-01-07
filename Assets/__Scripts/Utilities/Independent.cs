using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Independent : MonoBehaviour
{

    public float destroyAfter = 8f;

    public bool followParent = true;
    private GameObject parentToFollow;

    void Awake() {
        parentToFollow = transform.parent.gameObject;
        transform.SetParent(null);
        Destroy(gameObject, destroyAfter);
    }

    private void Update() {
        if (followParent && parentToFollow != null) {
            transform.position = parentToFollow.transform.position;
        }
    }
}

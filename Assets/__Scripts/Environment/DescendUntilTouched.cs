using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DescendUntilTouched : MonoBehaviour {
    public float speed = 5f; // Vitesse de descente
    private bool hasBeenTouched = false; // Etat du contact

    void Update() {
        // Si l'objet n'a pas encore été touché, il descend
        if (!hasBeenTouched) {
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
        }
    }

    // Cette fonction est appelée lorsqu'un autre collider entre en collision avec celui de cet objet
    private void OnTriggerEnter(Collider collision) {
        // On marque l'objet comme ayant été touché
        if(collision.gameObject.layer == 0 || collision.gameObject.layer == 6) hasBeenTouched = true;
    }

}
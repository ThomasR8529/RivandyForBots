using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropDetection : MonoBehaviour
{
    Drop drop;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (drop)
        {
            drop.distance = Vector3.Distance(transform.position, drop.transform.position);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        // S'il s'agit d'un drop
        if (other.gameObject.layer == 8)
        {
            Drop dropFounded = other.GetComponent<Drop>();

            // Si un drop est déjà sélectionné.
            if (drop)
            {
                // Nous devons voir si la distance de ce nouveau objet est plus proche que l'ancien
                // Si la distance est plus proche, on prendre le nouveau.
                if( Vector3.Distance(dropFounded.transform.position, transform.position) < drop.distance)
                {
                    drop = dropFounded;
                }

                // Dans le cas contraire, on ne fait rien.
            }
            // Nous affichons l'UI qui propose de ramasser l'objet avec le nom du drop.

            // S'il n'y a pas encore de drop prédéfini, le drop affiché sera celui ci:
            if (drop is null)
            {
                drop = dropFounded;
            }
        }
    }
}

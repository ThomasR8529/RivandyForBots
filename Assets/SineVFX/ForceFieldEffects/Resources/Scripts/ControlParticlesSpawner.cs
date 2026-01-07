using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlParticlesSpawner : MonoBehaviour
{

    public ParticleSystem cps;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 10 || collision.gameObject.layer == 11)
        {
            Destroy(collision.gameObject);
            cps.transform.position = collision.transform.position;
            cps.Emit(1);
        }
    }
}

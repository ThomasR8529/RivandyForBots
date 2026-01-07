using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ParticleToActivate {
    public ParticleSystem _particle;
    public float seconds;
}

[Serializable]
public class ColliderToActivate {
    public Collider _collider;
    public float seconds;
}

[Serializable]
public class AudioToActivate {
    public AudioSource _audioSource;
    public float seconds;
}

public class ParticleCollisionEnableAfter : MonoBehaviour
{
    [SerializeField] List<ParticleToActivate> particleList;
    [SerializeField] List<ColliderToActivate> colliderList;
    [SerializeField] List<AudioToActivate> audioList;

    private void Start() {
        // Parcourir chaque ParticleToActivate dans particleList et démarrer une coroutine pour chacun
        foreach (ParticleToActivate particleToActivate in particleList) {
            var collision = particleToActivate._particle.collision;
            collision.enabled = false;
            StartCoroutine(ActivateParticleCollisionAfterDelay(particleToActivate._particle, particleToActivate.seconds));
        }
        foreach (ColliderToActivate colliderToActivate in colliderList) {
            colliderToActivate._collider.enabled = false;
            StartCoroutine(ActivateColliderAfterDelay(colliderToActivate._collider, colliderToActivate.seconds));
        }
        foreach (AudioToActivate audioToActivate in audioList) {
            StartCoroutine(ActivateAudioAfterDelay(audioToActivate._audioSource, audioToActivate.seconds));
        }
    }

    private IEnumerator ActivateParticleCollisionAfterDelay(ParticleSystem particle, float delay) {
        // Attendre le délai spécifié
        yield return new WaitForSeconds(delay);

        // Activer le module de collision du système de particules
        var collisionModule = particle.collision;
        collisionModule.enabled = true;
    }
    private IEnumerator ActivateColliderAfterDelay(Collider collider, float delay) {
        // Attendre le délai spécifié
        yield return new WaitForSeconds(delay);
        collider.enabled = true;
    }
    private IEnumerator ActivateAudioAfterDelay(AudioSource audioSource, float delay) {
        // Attendre le délai spécifié
        yield return new WaitForSeconds(delay);
        audioSource.Play();
    }
}

using UnityEngine;

public class AlignParticlesToGround : MonoBehaviour {
    public LayerMask groundLayer;
    public float offset = 0.1f; // Ajustez cela en fonction de la hauteur de la sphère

    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;

    void Start() {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    void LateUpdate() {
        int numParticlesAlive = particleSystem.GetParticles(particles);

        for (int i = 0 ; i < numParticlesAlive ; i++) {
            Vector3 particlePosition = particles[i].position;

            RaycastHit hit;
            if (Physics.Raycast(particlePosition, Vector3.down, out hit, Mathf.Infinity, groundLayer)) {
                particlePosition.y = hit.point.y + offset;
            }

            particles[i].position = particlePosition;
        }

        particleSystem.SetParticles(particles, numParticlesAlive);
    }
}
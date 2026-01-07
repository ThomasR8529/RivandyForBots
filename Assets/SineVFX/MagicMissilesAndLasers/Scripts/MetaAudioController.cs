using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MetaAudioController : MonoBehaviour
{

    public AudioSource loopingSFX;
    public GameObject[] waveSfxPrefabs;
    public GameObject[] explosionSfxPregabs;
    public GameObject[] smallExplosionSfxPregabs;
    public float globalProgressSpeed = 1f;

    private float globalProgress;

    void Start()
    {

        globalProgress = 0f;

    }

    // Spawn Empty with Sound FX
    public void EmitParticleExplosion(Vector3 pos, bool big)
    {
        if (big == true)
        {
            Instantiate(explosionSfxPregabs[Random.Range(0, explosionSfxPregabs.Length)], pos, transform.rotation);
        }
        else
        {
            Instantiate(smallExplosionSfxPregabs[Random.Range(0, smallExplosionSfxPregabs.Length)], pos, transform.rotation);
        }
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                globalProgress = 1f;
            }

            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                Instantiate(waveSfxPrefabs[Random.Range(0, waveSfxPrefabs.Length)], transform.position, transform.rotation);
            }
        }
#else
        if (Input.GetMouseButton(0))
        {
            globalProgress = 1f;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Instantiate(waveSfxPrefabs[Random.Range(0, waveSfxPrefabs.Length)], transform.position, transform.rotation);
        }
        #endif

        if (globalProgress >= 0f)
        {
            globalProgress -= Time.deltaTime * globalProgressSpeed;
        }

        loopingSFX.volume = globalProgress;
    }
}

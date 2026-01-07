using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EGA_EffectSound : MonoBehaviour
{
    [SerializeField]
    private float StartTime = 0.0f;
    [SerializeField]
    private AudioClip clip;
    [SerializeField]
    private AudioSource soundComponent;

    void Start ()
    {
        soundComponent = GetComponent<AudioSource>();
        clip = soundComponent.clip;
        soundComponent.PlayOneShot(clip);
    }
}

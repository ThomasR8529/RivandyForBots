using UnityEngine;
using UnityEngine.Playables;

// Plays a Timeline (PlayableDirector) and sounds at Survivor round start/end
// Attach this to a GameObject in your gameplay/UI scene and assign the fields.
public class SurvivorRoundAnnouncer : MonoBehaviour
{
#if !UNITY_SERVER
    [Header("Timelines")]
    [Tooltip("Timeline played when a new wave starts (Survivor mode). Optional.")]
    [SerializeField] private PlayableDirector waveStartDirector;

    [Tooltip("Timeline played when a wave ends (bonus selection opens). Optional.")]
    [SerializeField] private PlayableDirector waveEndDirector;

    [Header("Audio (SoundManager keys)")]
    [Tooltip("Sound key played at wave start (uses SoundManager.Play2D)")]
    [SerializeField] private string waveStartSoundKey;

    [Tooltip("Sound key played only on wave 1 (falls back to waveStartSoundKey if empty)")]
    [SerializeField] private string firstWaveStartSoundKey;

    [Tooltip("Sound key played at wave end (uses SoundManager.Play2D)")]
    [SerializeField] private string waveEndSoundKey;

    [Header("Behaviour")]
    [Tooltip("If true, plays the start timeline/sound when the first wave becomes active.")]
    [SerializeField] private bool playOnFirstWave = true;

    private bool _firstWavePlayed;
    private bool _grantedInitialHeal;

    private void OnEnable()
    {
        Zone.OnWaveStartedClient += HandleWaveStarted;
        Zone.OnWaveEndedClient += HandleWaveEnded;
    }

    private void OnDisable()
    {
        Zone.OnWaveStartedClient -= HandleWaveStarted;
        Zone.OnWaveEndedClient -= HandleWaveEnded;
    }

    private void HandleWaveStarted(int wave)
    {
        Debug.Log("Wave started");
        if (!playOnFirstWave && !_firstWavePlayed)
        {
            _firstWavePlayed = true;
            return;
        }
        _firstWavePlayed = true;
        if (wave == 1 && !string.IsNullOrEmpty(firstWaveStartSoundKey))
        {
            PlayWaveStart(firstWaveStartSoundKey);
        }
        else
        {
            PlayWaveStart();
        }
    }

    private void HandleWaveEnded(int wave)
    {
        PlayWaveEnd();
    }

    private void PlayWaveStart(string overrideSoundKey = null)
    {
        if (waveStartDirector != null)
        {
            waveStartDirector.time = 0;
            waveStartDirector.Evaluate();
            waveStartDirector.Play();
        }
        string soundKey = string.IsNullOrEmpty(overrideSoundKey) ? waveStartSoundKey : overrideSoundKey;
        if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play2D(soundKey);
        }
    }

    private void PlayWaveEnd()
    {
        if (waveEndDirector != null)
        {
            waveEndDirector.time = 0;
            waveEndDirector.Evaluate();
            waveEndDirector.Play();
        }
        if (!string.IsNullOrEmpty(waveEndSoundKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play2D(waveEndSoundKey);
        }
    }
#endif
}

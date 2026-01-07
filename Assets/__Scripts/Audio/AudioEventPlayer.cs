using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[AddComponentMenu("Rivandy/Audio/Audio Event Player")]
public class AudioEventPlayer : MonoBehaviour
{
    [Serializable]
    public class AudioAction
    {
        [Header("Clé audio")]
        [Tooltip("voice_key définie dans AudioKeyConfig")]
        public string voiceKey = "hit";

        [Header("Lecture")]
        public bool loop = false;
        [Range(0f, 1f)] public float volume = -1f;   // -1 => défaut de la clé
        [Range(-3f, 3f)] public float pitch = -999f; // -999 => défaut de la clé
        [Tooltip("Temps de départ dans le clip (secondes)")]
        public float startTime = 0f;
        [Tooltip("Anti-spam par clé (en secondes)")]
        public float cooldown = 0f;
        [Tooltip("Fondu d’entrée (secondes)")]
        public float fadeIn = 0f;

        [Header("Routage (optionnel)")]
        [Tooltip("Surcharge du AudioMixerGroup. Laisse vide pour utiliser celui du type (FX/Voice/...)")]
        public AudioMixerGroup overrideMixer;

        [Header("Spatialisation")]
        public Mode mode = Mode.TwoD;
        public enum Mode { TwoD, AtPoint, AttachedToThis, AttachedToTarget }

        [Tooltip("Si Mode=AttachedToTarget")]
        public Transform attachTarget;

        [Tooltip("Si Mode=AtPoint")]
        public Vector3 worldPosition;

        [Tooltip("0=2D, 1=3D. Si <0, valeur auto (0 en 2D, 1 en 3D)")]
        [Range(-0.1f, 1f)] public float spatialBlend = -0.01f;

        [Header("Distances 3D")]
        [Tooltip("<=0 pour utiliser les valeurs par défaut du SoundManager")]
        public float minDistance = -1f;
        [Tooltip("<=0 pour utiliser les valeurs par défaut du SoundManager")]
        public float maxDistance = -1f;
        public AudioRolloffMode rolloffMode = 0; // 0 signifie: laisser le SoundManager décider
        [Range(0f, 360f)] public float spread = 0f;
        [Range(0f, 5f)] public float doppler = 0f;
        [Range(0, 256)] public int priority = 128;
    }

    [Header("On Enable Event ()")]
    public List<AudioAction> onEnableActions = new();

    [Header("On Start Event ()")]
    public List<AudioAction> onStartActions = new();

    [Header("On Disable Event ()")]
    public List<AudioAction> onDisableActions = new();

    [Header("On Destroy Event ()")]
    public List<AudioAction> onDestroyActions = new();


    [Header("On Destroy Stop Keys")]
    [Tooltip("Liste des clés à arrêter quand l'objet est détruit.")]
    public List<string> stopKeysOnDestroy = new();

    private void OnDestroy()
    {
        PlayList(onDestroyActions);

        if (SoundManager.Instance != null && stopKeysOnDestroy != null)
        {
            foreach (var key in stopKeysOnDestroy)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    SoundManager.Instance.StopByKey(key, SoundManager.StopMode.Immediate);
                }
            }
        }
    }

    // --- Unity Events ---
    private void OnEnable() { PlayList(onEnableActions); }
    private void Start() { PlayList(onStartActions); }
    private void OnDisable() { PlayList(onDisableActions); }

    // --- API publique pour UnityEvent/Timeline/etc ---
    public void PlayByKey(string key)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.Play2D(key); // helper simple 2D
    }

    public void PlayCustom(AudioAction cfg)
    {
        PlayOne(cfg);
    }

    // --- Implémentation ---
    private void PlayList(List<AudioAction> list)
    {
        if (list == null || list.Count == 0) return;
        foreach (var a in list) PlayOne(a);
    }

    private void PlayOne(AudioAction a)
    {
        if (SoundManager.Instance == null || string.IsNullOrWhiteSpace(a.voiceKey))
            return;

        var opts = new SoundManager.PlayOptions
        {
            loop = a.loop,
            volume = a.volume,
            pitch = a.pitch,
            startTime = a.startTime,
            cooldownSeconds = a.cooldown,
            fadeInSeconds = a.fadeIn,

            // routing
            overrideMixer = a.overrideMixer,

            // spatial
            spatial = a.mode != AudioAction.Mode.TwoD,
            spatialBlend = a.spatialBlend,
            minDistance = a.minDistance,
            maxDistance = a.maxDistance,
            rolloffMode = a.rolloffMode,
            spread = a.spread,
            dopplerLevel = a.doppler,
            priority = a.priority
        };

        switch (a.mode)
        {
            case AudioAction.Mode.TwoD:
                SoundManager.Instance.Play(a.voiceKey, opts);
                break;

            case AudioAction.Mode.AtPoint:
                opts.position = a.worldPosition;
                SoundManager.Instance.Play(a.voiceKey, opts);
                break;

            case AudioAction.Mode.AttachedToThis:
                opts.attachTo = transform;
                SoundManager.Instance.Play(a.voiceKey, opts);
                break;

            case AudioAction.Mode.AttachedToTarget:
                opts.attachTo = a.attachTarget != null ? a.attachTarget : transform;
                SoundManager.Instance.Play(a.voiceKey, opts);
                break;
        }
    }
}
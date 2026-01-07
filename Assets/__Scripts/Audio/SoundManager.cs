using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private AudioKeyConfig config;

    [Header("Options / Volumes")]
    [Tooltip("Mixer principal avec les parametres exposes de volume")]
    [SerializeField] private AudioMixer mainMixer;
    [Tooltip("Parametre expose pour le Master (dB)")]
    public string masterParam = "MasterVolume";
    [Tooltip("Parametre expose pour la Musique (dB)")]
    public string musicParam = "MusicVolume";
    [Tooltip("Parametre expose pour les SFX (dB)")]
    public string sfxParam = "SFXVolume";
    [Tooltip("Parametre expose pour l'UI (dB)")]
    public string uiParam = "UIVolume";
    [Tooltip("Parametre expose pour les Voix (dB)")]
    public string voiceParam = "VoiceVolume";
    [Tooltip("Parametre expose pour l'Ambiance (dB)")]
    public string ambientParam = "AmbientVolume";

    private const string PP_MASTER = "volume_master";
    private const string PP_MUSIC = "volume_music";
    private const string PP_SFX = "volume_sfx";
    private const string PP_UI = "volume_ui";
    private const string PP_VOICE = "volume_voice";
    private const string PP_AMBI = "volume_ambient";

    [Header("Mixers par type (défauts)")]
    public AudioMixerGroup mixerAmbient;
    public AudioMixerGroup mixerFX;
    public AudioMixerGroup mixerVoice;
    public AudioMixerGroup mixerUI;
    public AudioMixerGroup mixerMusic;

    [Header("Pool / Perf")]
    [SerializeField] private int pooledSources = 24;
    [SerializeField] private bool expandPoolIfNeeded = true;

    [Header("Distances par défaut (3D)")]
    public float defaultMinDistance = 1f;
    public float defaultMaxDistance = 25f;
    public AudioRolloffMode defaultRolloff = AudioRolloffMode.Logarithmic;

    // --- interne ---
    private readonly Queue<AudioSource> _free = new();
    private readonly HashSet<AudioSource> _busy = new();
    private readonly Dictionary<Guid, AudioSource> _handles = new();
    private readonly Dictionary<string, float> _cooldowns = new(); // anti-spam par clé
    private Transform _poolRoot;

    // --- Structures publiques ---
    [Serializable]
    public struct PlayOptions
    {
        // 2D/3D
        public bool spatial;                  // false = 2D (spatialBlend = 0)
        public Vector3? position;             // si spatial et non attaché
        public Transform attachTo;            // si attaché à un Transform

        // Lecture
        public bool loop;
        public float volume;                  // -1 => use default
        public float pitch;                   // -999 => use default
        public float startTime;               // en secondes (0 par défaut)

        // Spatialisation avancée (si spatial)
        public float spatialBlend;            // 0..1 (si <0, on choisit 1 pour spatial, 0 pour 2D)
        public float minDistance;
        public float maxDistance;
        public AudioRolloffMode rolloffMode;
        public float spread;
        public float dopplerLevel;
        public int priority;                  // 0-256

        // Routing
        public AudioMixerGroup overrideMixer;

        // Sécurité / UX
        public float cooldownSeconds;         // anti-spam par clé
        public float fadeInSeconds;           // fondu d'entrée
    }

    public enum StopMode { Immediate, FadeOut }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
            Debug.LogWarning("[SoundManager] Config manquante (AudioKeyConfig).");

        // init pool
        _poolRoot = new GameObject("AudioPool").transform;
        _poolRoot.SetParent(transform, worldPositionStays: false);
        for (int i = 0; i < pooledSources; i++)
            _free.Enqueue(CreatePooledSource());

    }

    void Start()
    {
        // appliquer les volumes sauvegardes
        ApplyAllVolumesFromPrefs();
    }
    private AudioSource CreatePooledSource()
    {
        var go = new GameObject("AudioSource");
        go.transform.SetParent(_poolRoot, worldPositionStays: false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialize = false;
        src.spatialBlend = 0f;
        src.rolloffMode = defaultRolloff;
        src.minDistance = defaultMinDistance;
        src.maxDistance = defaultMaxDistance;
        src.dopplerLevel = 0f;
        src.priority = 128;
        return src;
    }

    private AudioSource AcquireSource()
    {
        if (_free.Count > 0) return _free.Dequeue();
        if (!expandPoolIfNeeded) return null;
        return CreatePooledSource();
    }

    private void ReleaseSource(AudioSource src)
    {
        if (src == null) return;
        _busy.Remove(src);
        src.Stop();
        src.clip = null;
        src.transform.SetParent(_poolRoot, false);
        src.transform.localPosition = Vector3.zero;
        src.loop = false;
        src.outputAudioMixerGroup = null;
        src.volume = 1f;
        src.pitch = 1f;
        src.spatialBlend = 0f;
        src.minDistance = defaultMinDistance;
        src.maxDistance = defaultMaxDistance;
        src.rolloffMode = defaultRolloff;
        src.spread = 0f;
        src.dopplerLevel = 0f;
        src.priority = 128;
        src.spatialize = false;
        _free.Enqueue(src);
    }

    // -------------------------------------------------
    // Résolution des clips & paramètres par défaut
    // -------------------------------------------------

    private bool TryResolve(string key, out AudioClip clip, out AudioKeyConfig.VoiceKeyEntry meta)
    {
        clip = null;
        meta = null;

        if (config == null)
            return false;

        string lang = PlayerPrefs.GetString("language", config.defaultLanguageCode);
        clip = config.ResolveClip(key, lang, out meta);

        if (meta == null)
        {
            Debug.LogWarning($"[SoundManager] Clé inconnue: {key}");
            return false;
        }

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] Aucun clip disponible pour '{key}' (langue '{lang}'), même en fallback.");
            return false;
        }

        // cooldown anti-spam
        if (_cooldowns.TryGetValue(key, out float until))
        {
            if (Time.unscaledTime < until) return false;
        }

        return true;
    }

    private AudioMixerGroup ResolveMixer(AudioKeyConfig.VoiceKeyEntry meta, AudioMixerGroup callerOverride)
    {
        if (callerOverride != null) return callerOverride;
        if (meta != null && meta.overrideMixer != null) return meta.overrideMixer;

        return meta?.type switch
        {
            AudioKeyConfig.AudioKeyType.Ambient => mixerAmbient,
            AudioKeyConfig.AudioKeyType.FX => mixerFX,
            AudioKeyConfig.AudioKeyType.Voice => mixerVoice,
            AudioKeyConfig.AudioKeyType.UI => mixerUI,
            AudioKeyConfig.AudioKeyType.Music => mixerMusic,
            _ => null
        };
    }

    // -------------------------------------------------
    // Volumes / Options
    // -------------------------------------------------

    private AudioMixer ResolveMainMixer()
    {
        if (mainMixer != null) return mainMixer;
        if (mixerMusic != null && mixerMusic.audioMixer != null) return mixerMusic.audioMixer;
        if (mixerFX != null && mixerFX.audioMixer != null) return mixerFX.audioMixer;
        if (mixerUI != null && mixerUI.audioMixer != null) return mixerUI.audioMixer;
        if (mixerVoice != null && mixerVoice.audioMixer != null) return mixerVoice.audioMixer;
        if (mixerAmbient != null && mixerAmbient.audioMixer != null) return mixerAmbient.audioMixer;
        return null;
    }

    private static float LinearToDb(float value01)
    {
        if (value01 <= 0.0001f) return -80f; // silence
        return Mathf.Log10(Mathf.Clamp01(value01)) * 20f;
    }

    private void SetMixerVolume(string exposedParam, float value01, string prefsKey)
    {
        var mixer = ResolveMainMixer();
        if (mixer == null)
        {
            Debug.LogWarning("[SoundManager] Aucun AudioMixer trouve pour appliquer les volumes.");
            return;
        }

        float db = LinearToDb(value01);
        bool applied = mixer.SetFloat(exposedParam, db);
        if (!applied)
        {
            Debug.LogWarning($"[SoundManager] Paramètre exposé introuvable: '{exposedParam}' sur mixer '{mixer?.name}'. Assurez-vous d'avoir exposé le volume du groupe et que le nom correspond exactement.");
            return;
        }
        if (!string.IsNullOrEmpty(prefsKey))
            PlayerPrefs.SetFloat(prefsKey, Mathf.Clamp01(value01));
    }

    // Optionnel: outil de validation des paramètres exposés (menu contextuel sur le composant)
    [ContextMenu("Validate Mixer Exposed Params")]
    private void ValidateMixerExposedParams()
    {
        var mixer = ResolveMainMixer();
        if (mixer == null)
        {
            Debug.LogWarning("[SoundManager] Aucun AudioMixer trouvé pour validation.");
            return;
        }

        void Check(string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return;
            if (!mixer.GetFloat(paramName, out _))
            {
                Debug.LogWarning($"[SoundManager] Paramètre exposé manquant sur mixer '{mixer.name}': '{paramName}'.");
            }
        }

        Check(masterParam);
        Check(musicParam);
        Check(sfxParam);
        Check(uiParam);
        Check(voiceParam);
        Check(ambientParam);
    }

    public void ApplyAllVolumesFromPrefs()
    {
        float master = PlayerPrefs.GetFloat(PP_MASTER, 1f);
        float music = PlayerPrefs.GetFloat(PP_MUSIC, 1f);
        float sfx = PlayerPrefs.GetFloat(PP_SFX, 1f);
        float ui = PlayerPrefs.GetFloat(PP_UI, 1f);
        float voice = PlayerPrefs.GetFloat(PP_VOICE, 1f);
        float ambi = PlayerPrefs.GetFloat(PP_AMBI, 1f);

        var mixer = ResolveMainMixer();
        if (mixer == null) return;
        Debug.Log("MASTER " + master);
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
        SetUIVolume(ui);
        SetVoiceVolume(voice);
        SetAmbientVolume(ambi);
    }

    public void SetMasterVolume(float value01) => SetMixerVolume(masterParam, value01, PP_MASTER);
    public void SetMusicVolume(float value01) => SetMixerVolume(musicParam, value01, PP_MUSIC);
    public void SetSFXVolume(float value01) => SetMixerVolume(sfxParam, value01, PP_SFX);
    public void SetUIVolume(float value01) => SetMixerVolume(uiParam, value01, PP_UI);
    public void SetVoiceVolume(float value01) => SetMixerVolume(voiceParam, value01, PP_VOICE);
    public void SetAmbientVolume(float value01) => SetMixerVolume(ambientParam, value01, PP_AMBI);

    public float GetMasterVolume() => PlayerPrefs.GetFloat(PP_MASTER, 1f);
    public float GetMusicVolume() => PlayerPrefs.GetFloat(PP_MUSIC, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(PP_SFX, 1f);
    public float GetUIVolume() => PlayerPrefs.GetFloat(PP_UI, 1f);
    public float GetVoiceVolume() => PlayerPrefs.GetFloat(PP_VOICE, 1f);
    public float GetAmbientVolume() => PlayerPrefs.GetFloat(PP_AMBI, 1f);

    public void SetVolumeByType(AudioKeyConfig.AudioKeyType type, float value01)
    {
        switch (type)
        {
            case AudioKeyConfig.AudioKeyType.Music: SetMusicVolume(value01); break;
            case AudioKeyConfig.AudioKeyType.FX: SetSFXVolume(value01); break;
            case AudioKeyConfig.AudioKeyType.UI: SetUIVolume(value01); break;
            case AudioKeyConfig.AudioKeyType.Voice: SetVoiceVolume(value01); break;
            case AudioKeyConfig.AudioKeyType.Ambient: SetAmbientVolume(value01); break;
            default: SetMasterVolume(value01); break;
        }
    }

    // -------------------------------------------------
    // API Publique — Helpers rapides
    // -------------------------------------------------

    /// <summary>Joue en 2D (one-shot).</summary>
    public Guid Play2D(string key, float volume = -1f, float pitch = -999f, float cooldown = 0f)
    {
        var opts = new PlayOptions
        {
            spatial = false,
            loop = false,
            volume = volume,
            pitch = pitch,
            cooldownSeconds = cooldown,
            spatialBlend = 0f,
            fadeInSeconds = 0f
        };
        return Play(key, opts);
    }
    public Guid PlayLoop2D(string key, float volume = -1f, float pitch = -999f, float cooldown = 0f)
    {
        var opts = new PlayOptions
        {
            spatial = false,
            loop = true,
            volume = volume,
            pitch = pitch,
            cooldownSeconds = cooldown,
            spatialBlend = 0f,
            fadeInSeconds = 0f
        };
        return Play(key, opts);
    }

    /// <summary>Joue à une position 3D (one-shot).</summary>
    public Guid PlayAtPoint(string key, Vector3 position, float volume = -1f, bool loop = false,
                            float minDist = -1f, float maxDist = -1f, float spatialBlend = 1f, float fadeIn = 0f)
    {
        var opts = new PlayOptions
        {
            spatial = true,
            position = position,
            loop = loop,
            volume = volume,
            spatialBlend = spatialBlend,
            minDistance = minDist,
            maxDistance = maxDist,
            rolloffMode = defaultRolloff,
            fadeInSeconds = fadeIn
        };
        return Play(key, opts);
    }

    /// <summary>Joue attaché à un Transform (3D).</summary>
    public Guid PlayAttached(string key, Transform attachTo, float volume = -1f, bool loop = false, float spatialBlend = 1f)
    {
        var opts = new PlayOptions
        {
            spatial = true,
            attachTo = attachTo,
            loop = loop,
            volume = volume,
            spatialBlend = spatialBlend
        };
        return Play(key, opts);
    }

    // -------------------------------------------------
    // API Publique — Appel complet
    // -------------------------------------------------

    public Guid Play(string key, PlayOptions options)
    {
        if (!TryResolve(key, out var clip, out var meta))
            return Guid.Empty;

        var src = AcquireSource();
        if (src == null)
        {
            Debug.LogWarning("[SoundManager] Pool épuisé et expansion désactivée.");
            return Guid.Empty;
        }

        // cooldown
        if (options.cooldownSeconds > 0f)
            _cooldowns[key] = Time.unscaledTime + options.cooldownSeconds;

        // routing
        src.outputAudioMixerGroup = ResolveMixer(meta, options.overrideMixer);

        // 2D / 3D
        bool spatial = options.spatial;
        src.spatialBlend = (options.spatialBlend < 0f) ? (spatial ? 1f : 0f) : Mathf.Clamp01(options.spatialBlend);
        src.spatialize = spatial;

        if (spatial)
        {
            src.rolloffMode = (options.rolloffMode == 0) ? defaultRolloff : options.rolloffMode;
            src.minDistance = (options.minDistance > 0f) ? options.minDistance : defaultMinDistance;
            src.maxDistance = (options.maxDistance > 0f) ? options.maxDistance : defaultMaxDistance;
            src.spread = Mathf.Max(0f, options.spread);
            src.dopplerLevel = Mathf.Max(0f, options.dopplerLevel);
            src.priority = Mathf.Clamp(options.priority == 0 ? 128 : options.priority, 0, 256);

            if (options.attachTo != null)
            {
                src.transform.SetParent(options.attachTo, worldPositionStays: false);
                src.transform.localPosition = Vector3.zero;
            }
            else if (options.position.HasValue)
            {
                src.transform.SetParent(_poolRoot, false);
                src.transform.position = options.position.Value;
            }
            else
            {
                // si spatial mais aucune position, on met (0,0,0)
                src.transform.SetParent(_poolRoot, false);
                src.transform.position = Vector3.zero;
            }
        }
        else
        {
            src.transform.SetParent(_poolRoot, false);
            src.transform.localPosition = Vector3.zero;
        }

        // paramètres audio
        src.clip = clip;
        src.loop = options.loop;
        src.volume = (options.volume < 0f) ? (meta?.defaultVolume ?? 1f) : Mathf.Clamp01(options.volume);
        src.pitch = (options.pitch < -900f) ? (meta?.defaultPitch ?? 1f) : Mathf.Clamp(options.pitch, -3f, 3f);

        // handle
        Guid handle = Guid.NewGuid();
        _handles[handle] = src;
        _busy.Add(src);

        // lecture (avec fade éventuel)
        if (options.startTime > 0f && options.startTime < src.clip.length)
            src.time = options.startTime;

        if (options.fadeInSeconds > 0f)
        {
            float targetVol = src.volume;
            src.volume = 0f;
            src.Play();
            StartCoroutine(FadeRoutine(src, 0f, targetVol, options.fadeInSeconds));
        }
        else
        {
            src.Play();
        }

        // si non loop, prévoir auto-release
        if (!src.loop)
            StartCoroutine(AutoReleaseWhenDone(handle, src));

        return handle;
    }

    // -------------------------------------------------
    // Stop / Fade / Utilitaires
    // -------------------------------------------------

    public void Stop(Guid handle, StopMode mode = StopMode.Immediate, float fadeSeconds = 0.2f)
    {
        if (!_handles.TryGetValue(handle, out var src) || src == null) return;
        if (mode == StopMode.Immediate)
        {
            _handles.Remove(handle);
            ReleaseSource(src);
        }
        else
        {
            StartCoroutine(StopWithFade(handle, src, fadeSeconds));
        }
    }

    public int StopByKey(string key, StopMode mode = StopMode.Immediate, float fadeSeconds = 0.2f)
    {
        // On ne trace pas les clés par source, mais on peut comparer le clip actuel avec la résolution de la clé.
        if (!TryResolve(key, out var clip, out _)) return 0;

        List<Guid> toStop = new();
        foreach (var kv in _handles)
        {
            var s = kv.Value;
            if (s != null && s.clip == clip)
                toStop.Add(kv.Key);
        }

        foreach (var h in toStop)
            Stop(h, mode, fadeSeconds);

        return toStop.Count;
    }

    public int StopAllOfType(AudioKeyConfig.AudioKeyType type, StopMode mode = StopMode.Immediate, float fadeSeconds = 0.2f)
    {
        List<Guid> toStop = new();
        foreach (var kv in _handles)
        {
            var s = kv.Value;
            if (s == null || s.clip == null) continue;
            // tentative: retrouver l'entry par nom — peu coûteux si indexé
            if (config.TryGetEntry(FindKeyByClip(s.clip), out var entry) && entry.type == type)
                toStop.Add(kv.Key);
        }

        foreach (var h in toStop)
            Stop(h, mode, fadeSeconds);

        return toStop.Count;
    }

    public void SetVolume(Guid handle, float volume01)
    {
        if (_handles.TryGetValue(handle, out var src) && src != null)
            src.volume = Mathf.Clamp01(volume01);
    }

    public void SetPitch(Guid handle, float pitch)
    {
        if (_handles.TryGetValue(handle, out var src) && src != null)
            src.pitch = Mathf.Clamp(pitch, -3f, 3f);
    }

    public bool IsPlaying(Guid handle)
    {
        return _handles.TryGetValue(handle, out var src) && src != null && src.isPlaying;
    }

    // -------------------------------------------------
    // Internes
    // -------------------------------------------------

    private IEnumerator AutoReleaseWhenDone(Guid handle, AudioSource src)
    {
        while (src != null && src.isPlaying)
            yield return null;

        if (src != null)
        {
            _handles.Remove(handle);
            ReleaseSource(src);
        }
    }

    private IEnumerator StopWithFade(Guid handle, AudioSource src, float fadeSeconds)
    {
        if (src == null) yield break;
        float start = src.volume;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / fadeSeconds);
            yield return null;
        }
        _handles.Remove(handle);
        ReleaseSource(src);
    }

    private IEnumerator FadeRoutine(AudioSource src, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds && src != null)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        if (src != null) src.volume = to;
    }

    // NOTE: utilitaire pour StopAllOfType — on mappe clip->key en cherchant dans la config.
    private string FindKeyByClip(AudioClip clip)
    {
        if (clip == null || config == null) return null;
        if (config.entries == null) return null;

        foreach (var e in config.entries)
        {
            if (e.isGlobal)
            {
                if (e.globalClip == clip) return e.key;
            }
            else
            {
                foreach (var lc in e.localizedClips)
                    if (lc != null && lc.clip == clip) return e.key;
            }
        }
        return null;
    }

    // Utilitaire si vous voulez recharger la langue manuellement après un changement extérieur.
    public void RefreshLanguageCache() => config?.BuildIndex();
}

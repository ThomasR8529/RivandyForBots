using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioKeyConfig", menuName = "Rivandy/Audio/AudioKeyConfig")]
public class AudioKeyConfig : ScriptableObject
{
    [Serializable]
    public enum AudioKeyType { Ambient, FX, Voice, UI, Music }

    [Serializable]
    public class LocalizedClip
    {
        [Tooltip("Code langue tel que stocké dans PlayerPrefs(\"language\"). Exemple: \"0\" pour EN, \"1\" pour FR, etc.")]
        public string languageCode = "0";
        public AudioClip clip;
    }

    [Serializable]
    public class VoiceKeyEntry
    {
        [Tooltip("Identifiant unique, ex: \"ui_matchmaking_waiting\" ou \"golem_roar\"")]
        public string key;

        public AudioKeyType type = AudioKeyType.Voice;

        [Header("Langues / Global")]
        [Tooltip("Si true, on utilise 'globalClip' pour toutes les langues, peu importe PlayerPrefs.")]
        public bool isGlobal = false;

        [Tooltip("Clip utilisé si 'isGlobal' = true")]
        public AudioClip globalClip;

        [Tooltip("Clips par langue. Ignorés si 'isGlobal' = true.")]
        public List<LocalizedClip> localizedClips = new();

        [Header("Défauts de lecture (peuvent être écrasés par l'appel)")]
        [Range(0f, 1f)] public float defaultVolume = 1f;
        [Range(-3f, 3f)] public float defaultPitch = 1f;

        [Tooltip("Mixer spécifique pour cette clé (sinon on utilise celui du type).")]
        public AudioMixerGroup overrideMixer;
    }

    [Header("Langue par défaut (si PlayerPrefs est manquant ou clip inexistant)")]
    public string defaultLanguageCode = "0";

    [Header("Clés disponibles")]
    public List<VoiceKeyEntry> entries = new();

    // --- Accès optimisé en runtime ---
    private Dictionary<string, VoiceKeyEntry> _map;

    public void BuildIndex()
    {
        if (_map == null) _map = new Dictionary<string, VoiceKeyEntry>(StringComparer.OrdinalIgnoreCase);
        else _map.Clear();

        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            if (_map.ContainsKey(e.key))
            {
                Debug.LogWarning($"[AudioKeyConfig] Clé dupliquée: {e.key}");
                continue;
            }
            _map.Add(e.key, e);
        }
    }

    public bool TryGetEntry(string key, out VoiceKeyEntry entry)
    {
        if (_map == null) BuildIndex();
        return _map.TryGetValue(key, out entry);
    }

    public AudioClip ResolveClip(string key, string languageCode, out VoiceKeyEntry meta)
    {
        meta = null;
        if (string.IsNullOrEmpty(key))
            return null;

        if (!TryGetEntry(key, out meta) || meta == null)
            return null;

        if (meta.isGlobal)
            return meta.globalClip;

        // langue exacte
        if (!string.IsNullOrEmpty(languageCode))
        {
            foreach (var lc in meta.localizedClips)
            {
                if (lc != null && lc.clip != null && string.Equals(lc.languageCode, languageCode, StringComparison.OrdinalIgnoreCase))
                    return lc.clip;
            }
        }

        // fallback sur langue par défaut
        foreach (var lc in meta.localizedClips)
        {
            if (lc != null && lc.clip != null && string.Equals(lc.languageCode, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
                return lc.clip;
        }

        // dernier recours: premier clip non nul
        foreach (var lc in meta.localizedClips)
        {
            if (lc != null && lc.clip != null)
                return lc.clip;
        }

        return null;
    }
}
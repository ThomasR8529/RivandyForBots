using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public class ModeIdGameObjectBootstrap : MonoBehaviour
{
    [Serializable]
    public class ModeToggle
    {
        public int modeId;
        public GameObject[] enable;
        public GameObject[] disable;
    }

    [SerializeField] private ModeToggle[] toggles;
    [SerializeField] private bool logDetails = true;

    private static bool _configLoaded;
    private static int _modeId = -1;
    private static string _configPathUsed;

    public static int ModeId => _modeId;
    public static string ConfigPathUsed => _configPathUsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreloadModeId()
    {
        LoadModeId();
    }

    private static void LoadModeId()
    {
        var primary = Path.Combine(Application.streamingAssetsPath, "server_config/config.json");
        var fallback = Path.Combine(Application.streamingAssetsPath, "server_config/config_example.json");
        var path = File.Exists(primary) ? primary : fallback;

        if (!File.Exists(path))
        {
            Debug.LogWarning("[ModeIdGameObjectBootstrap] No config.json found in StreamingAssets/server_config.");
            _modeId = -1;
            _configLoaded = true;
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonUtility.FromJson<ServerConfig>(json);
            _modeId = config != null ? config.modeId : -1;
            _configPathUsed = path;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModeIdGameObjectBootstrap] Failed to read {path}: {ex.Message}");
            _modeId = -1;
        }

        _configLoaded = true;
    }

    private void Awake()
    {
        if (!_configLoaded)
        {
            LoadModeId();
        }

        ApplyForMode(_modeId);

        enabled = false;
    }

    private void ApplyForMode(int modeId)
    {
        ModeToggle entry = null;
        if (toggles != null)
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                var toggle = toggles[i];
                if (toggle != null && toggle.modeId == modeId)
                {
                    entry = toggle;
                    break;
                }
            }
        }

        if (entry == null)
        {
            if (logDetails)
            {
                Debug.LogWarning($"[ModeIdGameObjectBootstrap] No mapping set for modeId {modeId}.");
            }
            return;
        }

        SetActive(entry.enable, true);
        SetActive(entry.disable, false);

        if (logDetails)
        {
            Debug.Log($"[ModeIdGameObjectBootstrap] Applied modeId {modeId} (config: {(_configPathUsed ?? "unknown")}).");
        }
    }

    private static void SetActive(GameObject[] targets, bool state)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            var go = targets[i];
            if (go == null) continue;
            go.SetActive(state);
        }
    }
}

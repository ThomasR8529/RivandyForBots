using UnityEngine;
#if !UNITY_SERVER
using Steamworks;
#endif

public class SteamStatsManager : MonoBehaviour
{
    public static SteamStatsManager Instance { get; private set; }

#if !UNITY_SERVER
    private Callback<UserStatsReceived_t> _userStatsReceived;
    private bool _statsReady;
#endif

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if !UNITY_SERVER
        // Defer setting up callbacks until first sync to avoid dependency on SteamManager type.
#endif
    }

#if !UNITY_SERVER
    private void OnUserStatsReceived(UserStatsReceived_t p)
    {
        // Ensure the callback is for the local user
        if (p.m_steamIDUser == SteamUser.GetSteamID())
        {
            _statsReady = true;
        }
    }
#endif

    public static void SyncLevelStat(int level)
    {
#if !UNITY_SERVER
        EnsureInstance();
        Instance.InternalSyncLevel(level);
#endif
    }

#if !UNITY_SERVER
    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("SteamStatsManager");
            go.AddComponent<SteamStatsManager>();
        }
    }

    private void InternalSyncLevel(int level)
    {
        // Only proceed if the Steam client is running; assumes SteamAPI.Init was called elsewhere.
        if (!SteamAPI.IsSteamRunning()) return;

        if (_userStatsReceived == null)
        {
            _userStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        }
        if (!_statsReady)
        {
            SteamUserStats.RequestCurrentStats();
        }

        // Update the level stat
        SteamUserStats.SetStat("level", level);

        // Sync level-based achievements (grant when met, revoke when not)
        SyncLevelAchievement(level, 10);
        SyncLevelAchievement(level, 25);
        SyncLevelAchievement(level, 50);
        SyncLevelAchievement(level, 100);
        SyncLevelAchievement(level, 300);
        SteamUserStats.StoreStats();
    }

    private static void SyncLevelAchievement(int level, int threshold)
    {
        // Try both common naming patterns to be resilient to dashboard naming
        string[] candidates = new string[] { $"ACH_LEVEL_{threshold}", $"ACH_LEVEL__{threshold}" };
        foreach (var apiName in candidates)
        {
            bool achieved;
            if (SteamUserStats.GetAchievement(apiName, out achieved))
            {
                Debug.Log("Player Lv.: " + level + ", threshold: " + threshold);
                if (level >= threshold)
                {
                    if (!achieved) SteamUserStats.SetAchievement(apiName);
                }
                else
                {
                    if (achieved) SteamUserStats.ClearAchievement(apiName);
                }
                break;
            }
        }
    }
#endif
}

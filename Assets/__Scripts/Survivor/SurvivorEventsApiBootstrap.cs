using UnityEngine;

public static class SurvivorEventsApiBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAdapter()
    {
        if (SurvivorEventsApi.Instance == null)
        {
            var go = new GameObject("SurvivorEventsApiAdapter(auto)");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SurvivorEventsApiAdapter>();
        }
    }
}


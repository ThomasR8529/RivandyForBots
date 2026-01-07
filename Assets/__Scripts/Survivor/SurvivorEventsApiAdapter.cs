using UnityEngine;

public class SurvivorEventsApiAdapter : MonoBehaviour, ISurvivorEventsApi
{
    private void Awake()
    {
        SurvivorEventsApi.Instance = this;
    }

    public void ReportChallengeFailed()
    {
        SurvivorMode.SurvivorEventSystem.Instance?.ReportChallengeFailed();
    }

    public void ReportChallengeSucceeded()
    {
        SurvivorMode.SurvivorEventSystem.Instance?.ReportChallengeSucceeded();
    }

    public void NotifyWaveStarted(int wave)
    {
        SurvivorMode.SurvivorEventSystem.Instance?.NotifyWaveStarted(wave);
    }

    public void NotifyWaveEnded(int wave)
    {
        SurvivorMode.SurvivorEventSystem.Instance?.NotifyWaveEnded(wave);
    }
}

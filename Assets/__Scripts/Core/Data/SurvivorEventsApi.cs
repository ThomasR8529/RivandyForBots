public interface ISurvivorEventsApi
{
    void ReportChallengeFailed();
    void ReportChallengeSucceeded();
    void NotifyWaveStarted(int wave);
    void NotifyWaveEnded(int wave);
}

public static class SurvivorEventsApi
{
    public static ISurvivorEventsApi Instance { get; set; }
}


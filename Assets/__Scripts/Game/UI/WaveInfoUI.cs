using TMPro;
using Unity.Netcode;
using UnityEngine;
using SurvivorMode;

public class WaveInfoUI : MonoBehaviour
{
    public static WaveInfoUI instance;

    [SerializeField] private TextMeshProUGUI killedMonstersText;
    [SerializeField] private TextMeshProUGUI aliveMonstersText;
    [SerializeField] private TextMeshProUGUI elapsedTimeText;
    [SerializeField] private TextMeshProUGUI timeToNextWaveText;
    [SerializeField] private LocalizedText waveNumberText;
    [SerializeField] private TextMeshProUGUI monsterBonusText;

    public float startTime;

    private Zone zone;
    private PlayerStatistics playerStats;
    private float lastServerTotalBonus = -1f; // Value pushed from server via ClientRpc

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnEnable()
    {
        Zone.OnWaveStartedClient += HandleWaveStartedClient;
    }

    private void OnDisable()
    {
        Zone.OnWaveStartedClient -= HandleWaveStartedClient;
    }

    private void HandleWaveStartedClient(int wave)
    {
        // Reset server-cached total so local fallback recomputes if needed
        lastServerTotalBonus = -1f;
    }
#if !UNITY_SERVER
    private void Update()
    {
        if (zone == null)
        {
            zone = Run.instance?.CPUcontroller?.zone;
        }
        if (playerStats == null)
        {
            playerStats = GameController.instance?.playerReference?.playerStatistics;
        }
        if (Run.instance?.CPUcontroller?.zone == null || GameController.instance?.playerReference?.playerStatistics == null) return;
        float elapsedTime = NetworkManager.Singleton.ServerTime.TimeAsFloat - this.startTime;
        elapsedTimeText.text = FormatTime(elapsedTime);

        float timeRemaining = GetTimeToNextWave(zone);
        timeToNextWaveText.text = FormatTime(timeRemaining);

        UpdateWaveInfo(zone, playerStats);
    }
#endif

    private void UpdateWaveInfo(Zone zone, PlayerStatistics playerStats)
    {
        waveNumberText.variables["roundNumber"] = zone.currentWave.ToString();
        waveNumberText.SetText(71);
        aliveMonstersText.text = zone.monsterNumber.ToString();
        killedMonstersText.text = playerStats.playerStatData.monsterKills.ToString();

        if (monsterBonusText != null)
        {
            if (lastServerTotalBonus >= 0f)
            {
                monsterBonusText.text = $"x{lastServerTotalBonus:0.##}";
                return;
            }

            // Fallback local computation aligned with server
            float totalBonus = SurvivorModifiers.GetMonsterScalingFactor(zone.currentWave);
            monsterBonusText.text = $"x{totalBonus:0.##}";
        }
    }

    private float GetTimeToNextWave(Zone zone)
    {
        return Mathf.Max(0, zone.waveEndTime - NetworkManager.Singleton.ServerTime.TimeAsFloat);
    }

    public void SetStartTime(float startTime)
    {
        this.startTime = startTime;
    }

    // Called from client RPC to apply server-authoritative total bonus
    public void ApplyServerTotalBonus(float totalBonus)
    {
        lastServerTotalBonus = totalBonus;
        if (monsterBonusText != null)
        {
            monsterBonusText.text = $"x{lastServerTotalBonus:0.##}";
        }
    }

    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
}

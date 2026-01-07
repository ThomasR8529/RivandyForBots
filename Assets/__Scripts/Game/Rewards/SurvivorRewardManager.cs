using UnityEngine;

public class SurvivorRewardManager : MonoBehaviour
{
    public static SurvivorRewardManager Instance;

    [SerializeField] private GameObject rewardPrefab;

    [SerializeField] private Transform container;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void ShowReward(int spiritId)
    {
        if (rewardPrefab == null || container == null)
            return;
        var spirit = GameDataController.instance.GetReincarnation(spiritId);
        if (spirit == null)
            return;
        GameObject obj = Instantiate(rewardPrefab, container);
        RewardUI ui = obj.GetComponent<RewardUI>();
        if (ui != null)
            ui.SetupSpirit(spirit);
    }
}

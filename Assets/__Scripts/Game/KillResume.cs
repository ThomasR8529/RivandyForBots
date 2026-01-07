using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillResume : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI winNameTMP;
    [SerializeField] private Image winIcon;
    [SerializeField] private TextMeshProUGUI loserNameTMP;
    [SerializeField] private Image loserIcon;

    /// <summary>
    /// Sets the winner and loser information.
    /// </summary>
    /// <param name="winner">The winning player.</param>
    /// <param name="loser">The losing player.</param>
    public void SetKillResume(PlayerReference winner, PlayerReference loser) {
        winNameTMP.text = winner.playerStatistics.playerDataGame.playerName.Value.ToString();
        winIcon.sprite = IconController.GetSpriteByAvatarId(winner.playerStatistics.playerDataGame.avatarId);

        loserNameTMP.text = loser.playerStatistics.playerDataGame.playerName.Value.ToString();
        loserIcon.sprite = IconController.GetSpriteByAvatarId(loser.playerStatistics.playerDataGame.avatarId);

        // Auto destroy after 3 seconds
        Destroy(gameObject, 3f);
    }
}

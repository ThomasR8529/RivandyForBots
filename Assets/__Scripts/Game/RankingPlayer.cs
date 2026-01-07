using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingPlayer : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI playerNameText;
    [SerializeField] TextMeshProUGUI killText;
    [SerializeField] TextMeshProUGUI soulText;

    [SerializeField] private TextMeshProUGUI rankedPointsText;
    [SerializeField] Image playerIcon;

    [SerializeField] private Image classIcon;
    [SerializeField] private Image spiritIcon;

    public void UpdateStats(string playerName, int kills, int soul, int avatarId, int classId, int reId, int rankedPoints)
    {
        if (playerNameText) playerNameText.text = playerName;
        if (killText) killText.text = kills.ToString();
        if (soulText) soulText.text = soul.ToString();

        if (playerIcon) playerIcon.sprite = IconController.GetSpriteByAvatarId(avatarId);
        if (classIcon && classId >= 0) classIcon.sprite = IconController.GetSpriteByClassId(classId);
        if (spiritIcon && reId >= 0) spiritIcon.sprite = IconController.GetSpriteByReId(reId);

        if (rankedPointsText && rankedPoints >= 0) rankedPointsText.text = rankedPoints.ToString();
    }


    public void UpdateBR(int rank, string playerName, int level, int rankedPoint, int avatarId)
    {
        playerNameText.text = $"#{rank} {playerName}";
        rankedPointsText.text = rankedPoint.ToString(); // à renommer visuel "Points" si tu veux
        killText.text = $"Lv.{level}";
        soulText.text = "";                     // ou cacher ce champ pour BR
        playerIcon.sprite = IconController.GetSpriteByAvatarId(avatarId);
        classIcon.gameObject.SetActive(false);
        spiritIcon.gameObject.SetActive(false);
    }

    public void UpdateSurvivor(int rank, string playerName, int level, int bestWave, int bestTimeSec, int avatarId)
    {
        playerNameText.text = $"#{rank} {playerName}";
        rankedPointsText.text = $"Wave {bestWave}";
        killText.text = FormatDuration(bestTimeSec);
        soulText.text = $"Lv.{level}";
        playerIcon.sprite = IconController.GetSpriteByAvatarId(avatarId);
        classIcon.gameObject.SetActive(false);
        spiritIcon.gameObject.SetActive(false);
    }
    private static string FormatDuration(int totalSeconds)
    {
        var t = System.TimeSpan.FromSeconds(totalSeconds);
        int h = (int)t.TotalHours;
        return h > 0
            ? $"{h:00}:{t.Minutes:00}:{t.Seconds:00}"   // >= 1h : hh:mm:ss
            : $"{t.Minutes:00}:{t.Seconds:00}";         // < 1h  : mm:ss
    }
}

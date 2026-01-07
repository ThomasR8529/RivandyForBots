using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class FeedbackEnemy : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI EnemyName;

    [SerializeField] private TextMeshProUGUI EnemyHealthText;
    [SerializeField] public TextMeshProUGUI EnemyShieldText;

    [SerializeField] public TextMeshProUGUI EnemySoulTMP;

    [SerializeField] private Slider EnemyHeath;
    [SerializeField] public Slider EnemyShield;

    [SerializeField] public Image avatarImg;

    [SerializeField] private Transform feedbackTransform;

    private Coroutine currentCoroutine;


    [Space(5)]
    [SerializeField] private PlayableDirector killEffectFB;
    [SerializeField] private PlayableDirector loseEffectFB;
    [SerializeField] private PlayableDirector winEffectFB;

    [SerializeField] private PlayableDirector survivorRecapEffectFB;

    public TextMeshProUGUI killEnemyName;
    public Image killEnemyLogo;
    public TextMeshProUGUI placeDefeat;

    [SerializeField] GameOverScreen gameOverScreen;

    private void Start()
    {
        feedbackTransform.gameObject.SetActive(false);
    }

    public void UpdateKillEffect(string enemyText, int avatarId)
    {
        killEnemyName.text = enemyText + " eliminated";
        killEnemyLogo.sprite = IconController.GetSpriteByAvatarId(avatarId);
    }

    public void UpdateDefeatEffect(int place)
    {
        string placeSuffix = GetPlaceSuffix(place);
        placeDefeat.text = place + placeSuffix + " place";
    }

    private string GetPlaceSuffix(int place)
    {
        if (place >= 11 && place <= 13)
        {
            return "th";
        }

        int lastDigit = place % 10;
        switch (lastDigit)
        {
            case 1:
                return "st";
            case 2:
                return "nd";
            case 3:
                return "rd";
            default:
                return "th";
        }
    }

    /// <summary>
    /// Updates the UI information for the enemy.
    /// </summary>
    /// <param name="enemyRef">The reference to the enemy player.</param>
    public void UpdateUiInformation(PlayerReference enemyRef)
    {
        //avatarImg.gameObject.SetActive(enemyRef.playerClasses.isMonster ? false: true);
        avatarImg.sprite = IconController.GetSpriteByAvatarId(enemyRef.playerStatistics.playerDataGame.avatarId);
        //EnemyShield.gameObject.SetActive(enemyRef.playerClasses.isMonster ? false : true);

        EnemyHeath.maxValue = enemyRef.playerStatistics.playerStatData.maxHealth;
        EnemyName.text = enemyRef.gameObject.name;
        EnemyHeath.value = enemyRef.playerStatistics.playerStatData.health;
        EnemyHealthText.text = Mathf.Round(enemyRef.playerStatistics.playerStatData.health).ToString();
        EnemyShieldText.text = Mathf.Round(enemyRef.playerStatistics.playerStatData.health).ToString();
        EnemyShield.maxValue = enemyRef.playerStatistics.playerStatData.maxHealth;
        EnemyShield.value = enemyRef.playerStatistics.playerStatData.shield;

        EnemySoulTMP.text = enemyRef.playerStatistics.playerStatData.soul.ToString();

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(DisableItInSeconds());
    }
    public IEnumerator DisableItInSeconds()
    {
        feedbackTransform.gameObject.SetActive(true);
        yield return new WaitForSeconds(5f);
        feedbackTransform.gameObject.SetActive(false);
    }

    public void PlayKillEffect()
    {
        StopIt();
        killEffectFB.gameObject.SetActive(true);
        killEffectFB.Play();
    }

    public void PlayLoseEffect()
    {
        loseEffectFB.gameObject.SetActive(true);
        loseEffectFB.Play();
    }

    public void PlayWinEffect()
    {
        StopIt();
        StartCoroutine(ActivateInTwoSeconds());
    }

    public void PlaySurvivorRecapEffect()
    {
        survivorRecapEffectFB.gameObject.SetActive(true);
        survivorRecapEffectFB.Play();
    }

    public void PlaySurvivorRecapEffect(
        PlayerStatistics playerStats,
        int waveRecord,
        bool isNewRecord,
        string gameStartTime,
        string gameDuration,
        int souls,
        int totalKills,
        float gainedExp,
        float totalExp,
        int playerLevel,
        int oldRankedpoint = -1,
        int newRankedPoint = -1)
    {
        int latestKills = totalKills;
        try { if (playerStats != null) latestKills = Mathf.Max(latestKills, playerStats.GetKills()); } catch { }
        // Mise à jour précise du GameOverScreen
        gameOverScreen.FillGameOverScreen(
            characterSprite: Resources.Load<Sprite>(GameDataController.instance.GetCharacter(playerStats.playerDataGame.classId).icon),
            spiritSprite: Resources.Load<Sprite>(GameDataController.instance.GetReincarnation(playerStats.playerDataGame.reId).icon),
            characterName: GameDataController.instance.GetCharacter(playerStats.playerDataGame.classId).name,
            spiritName: GameDataController.instance.GetReincarnation(playerStats.playerDataGame.reId).name,
            level: playerLevel,
            waveNumber: waveRecord,
            isNewRecord: isNewRecord,
            waveRecord: waveRecord,
            souls: souls,
            kills: latestKills,
            gameStartTime: gameStartTime,
            gameDuration: gameDuration,
            experienceGained: gainedExp,
            experience: totalExp,
            oldRankedpoint,
         newRankedPoint
        );

        survivorRecapEffectFB.gameObject.SetActive(true);
        survivorRecapEffectFB.Play();
        Cursor.lockState = CursorLockMode.Confined;
    }

    public IEnumerator ActivateInTwoSeconds()
    {
        yield return new WaitForSeconds(1.5f);
        winEffectFB.gameObject.SetActive(true);
        winEffectFB.Play();
    }

    public void StopIt()
    {
        StopAllCoroutines();
        feedbackTransform.gameObject.SetActive(false);
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class GameOverScreen : MonoBehaviour
{
    [Header("Character & Spirit")]
    public Image characterImage;
    public Image spiritImage;
    public TMP_Text characterNameTMP;
    public TMP_Text spiritNameTMP;

    [Header("Level & Experience")]
    public TMP_Text levelTMP;
    public Image experienceFillImage; // fillAmount entre 0 et 1

    public TMP_Text experienceDetailsTMP;
    public TMP_Text experienceGainTMP;
    public Image experienceGainedFillImage; // fillAmount entre 0 et 1

    [Header("Wave Information")]
    public LocalizedText waveNumberTMP;
    public GameObject newRecordIndicator;
    public LocalizedText waveRecordTMP;

    [Header("Game Stats")]
    public LocalizedText soulsTMP;
    public LocalizedText killsTMP;

    [Header("Game Time Information")]
    public LocalizedText gameStartTimeTMP;
    public LocalizedText gameDurationTMP;

    public TMP_Text rankedPointTMP;

    [Header("Animation Settings")]
    [Tooltip("Typing duration for names")] public float namesTypeDuration = 0.6f;
    [Tooltip("Duration for base XP fill")] public float xpFillDuration = 0.6f;
    [Tooltip("Duration for gained XP fill")] public float xpGainFillDuration = 0.5f;
    [Tooltip("Duration for level count")] public float levelCountDuration = 0.4f;
    [Tooltip("Duration for kills count")] public float killsCountDuration = 0.5f;
    [Tooltip("Duration for souls count")] public float soulsCountDuration = 0.5f;
    [Tooltip("Duration for ranked points count")] public float rankedPointsDuration = 0.6f;

    private Coroutine _sequenceCoroutine;

    public GraphicRaycaster graphicRaycaster;

    [Header("Skip Settings")]
    public Button skipButton; // optional: assign to enable click-to-skip
    public bool enableSkipKey = false;
    public KeyCode skipKey = KeyCode.Space;

    // Internal state for skip/finalization
    private bool _isAnimating = false;
    private bool _skipRequested = false;
    private string _tCharacterName, _tSpiritName;
    private int _tLevel, _tSouls, _tKills, _tOldRankedPoint, _tNewRankedPoint;
    private float _tExperience, _tExperienceGained;

    [Header("END Prefab Settings")]
    public GameObject endPrefab; // Assign Assets/SurvivorEventUI/Prefabs/END
    public Transform endSpawnParent; // optional parent in scene
    public Transform endSpawnPoint;  // optional position/rotation
    private GameObject _endInstance;

    [Header("Spectator Mode")]
    public Button spectatorButton; // optional: destroy END when clicked

    [Header("Spectate UI")]
    [SerializeField] private LocalizedText spectateTargetName; // Nom du joueur suivi côté client

    // Méthode pour remplir l'écran avec les données
    public void FillGameOverScreen(Sprite characterSprite, Sprite spiritSprite,
                                   string characterName, string spiritName,
                                   int level,
                                   int waveNumber, bool isNewRecord, int waveRecord,
                                   int souls, int kills,
                                   string gameStartTime, string gameDuration, float experienceGained, float experience, int oldRankedPoint = -1, int newRankedPoint = -1)
    {
        // Cache targets for skip/final state
        _tCharacterName = characterName;
        _tSpiritName = spiritName;
        _tLevel = level;
        _tSouls = souls;
        _tKills = kills;
        _tOldRankedPoint = oldRankedPoint;
        _tNewRankedPoint = newRankedPoint;
        _tExperience = experience;
        _tExperienceGained = experienceGained;

        // Images et noms
        Debug.Log(PlayerData.player.data.userId + " - kills: " + kills + ", place: " + waveNumber);
        characterImage.sprite = characterSprite;
        spiritImage.sprite = spiritSprite;
        characterNameTMP.text = string.Empty;
        spiritNameTMP.text = string.Empty;

        // Niveau et expérience
        levelTMP.text = "0";
        experienceFillImage.fillAmount = 0f;
        experienceGainedFillImage.fillAmount = 0f;
        experienceDetailsTMP.text = "0/50 XP";
        experienceGainTMP.text = "+0 XP";


        // Texte localisé : Numéro de vague
        waveNumberTMP.SetVariables(new Dictionary<string, string>
    {
        { "number", waveNumber.ToString() }
    });
        waveNumberTMP.SetText(Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale ? 121 : 75);
        TryLocalizedFallback(waveNumberTMP, Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale ? 121 : 75, " " + waveNumber);

        // Texte localisé : Record
        waveRecordTMP.SetVariables(new Dictionary<string, string>
    {
        { "record", waveRecord.ToString() }
    });
        waveRecordTMP.SetText(Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale ? 122 : 77);
        TryLocalizedFallback(waveRecordTMP, Run.instance.CPUcontroller.zone.serverMode == GameMode.BattleRoyale ? 122 : 77, " " + waveRecord);

        // Texte localisé : Heure de début
        gameStartTimeTMP.SetVariables(new Dictionary<string, string>
    {
        { "time", gameStartTime }
    });
        gameStartTimeTMP.SetText(78);
        TryLocalizedFallback(gameStartTimeTMP, 78, ": " + gameStartTime);

        // Texte localisé : Durée
        gameDurationTMP.SetVariables(new Dictionary<string, string>
    {
        { "duration", gameDuration }
    });
        gameDurationTMP.SetText(79);
        TryLocalizedFallback(gameDurationTMP, 79, ": " + gameDuration);

        soulsTMP.SetVariables(new Dictionary<string, string>
    {
        { "number", "0" }
    });
        killsTMP.SetVariables(new Dictionary<string, string>
    {
        { "number", "0" }
    });

        soulsTMP.SetText(73);
        killsTMP.SetText(74);
        // Fallbacks in case localized text lacks placeholders
        TryLocalizedFallback(killsTMP, 74, ": 0");
        TryLocalizedFallback(soulsTMP, 73, ": 0");
        if (rankedPointTMP)
            rankedPointTMP.text = (oldRankedPoint >= 0 && newRankedPoint >= 0) ? (oldRankedPoint + " > " + newRankedPoint + " RP") : string.Empty;
        // Wave

        newRecordIndicator.SetActive(isNewRecord);
        // Bind skip button if provided
        if (skipButton)
        {
            skipButton.onClick.RemoveListener(SkipAnimations);
            skipButton.onClick.AddListener(SkipAnimations);
        }
        if (spectatorButton)
        {
            spectatorButton.onClick.RemoveListener(OnSpectatorModeClicked);
            spectatorButton.onClick.AddListener(OnSpectatorModeClicked);
        }

        // Start animations sequence
        _skipRequested = false;
        _isAnimating = true;
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(AnimateSequence(
            characterName, spiritName,
            level,
            experience, experienceGained,
            souls, kills,
            oldRankedPoint, newRankedPoint));


    }

    private void TrySpawnEndPrefab(int classId, int reId)
    {
        if (graphicRaycaster != null) graphicRaycaster.enabled = true;
        if (endPrefab == null) return;
        if (_endInstance == null)
        {
            _endInstance = Instantiate(endPrefab, endSpawnParent != null ? endSpawnParent : null);
            if (endSpawnPoint != null)
            {
                _endInstance.transform.position = endSpawnPoint.position;
                _endInstance.transform.rotation = endSpawnPoint.rotation;
                _endInstance.transform.localScale = endSpawnPoint.localScale;
            }
        }

        // Activate only the mesh matching player's classId under child named "Base"
        Transform baseRoot = _endInstance.transform.Find("Base");
        if (baseRoot == null)
        {
            Debug.LogWarning("END prefab: 'Base' child not found.");
            return;
        }
        for (int i = 0; i < baseRoot.childCount; i++)
        {
            baseRoot.GetChild(i).gameObject.SetActive(false);
        }
        Transform child = baseRoot.GetChild(classId);
        child.gameObject.SetActive(true);


        Transform reRoot = _endInstance.transform.Find("Reincarnations");
        if (reRoot == null)
        {
            Debug.LogWarning("END prefab: 'Reincarnations' child not found.");
            return;
        }
        for (int i = 0; i < reRoot.childCount; i++)
        {
            reRoot.GetChild(i).gameObject.SetActive(false);
        }
        Transform childspirit = reRoot.GetChild(reId);
        childspirit.gameObject.SetActive(true);
    }

    private IEnumerator AnimateSequence(string characterName, string spiritName,
        int level, float experience, float experienceGained, int souls, int kills, int oldRankedPoint, int newRankedPoint)
    {
        yield return new WaitForSeconds(1.5f);
        // Spawn END prefab in scene (once)
        TrySpawnEndPrefab(PlayerData.player.data.classId, PlayerData.player.data.reId);

        yield return new WaitForSeconds(1.5f);
        // Names typing (parallel)
        IEnumerator t1 = TypeText(characterNameTMP, characterName, namesTypeDuration);
        IEnumerator t2 = TypeText(spiritNameTMP, spiritName, namesTypeDuration);
        Coroutine c1 = characterNameTMP ? StartCoroutine(t1) : null;
        Coroutine c2 = spiritNameTMP ? StartCoroutine(t2) : null;
        if (c1 != null) yield return c1;
        if (c2 != null) yield return c2;

        // Kills (0 -> kills)
        yield return AnimateIntLocalized(killsTMP, 74, 0, Mathf.Max(0, kills), killsCountDuration);
        // Souls (0 -> souls)
        yield return AnimateIntLocalized(soulsTMP, 73, 0, Mathf.Max(0, souls), soulsCountDuration);
        // Base XP fill and text (0 -> experience)
        float xpTargetFill = Mathf.Clamp01((50f > 0f ? (experience / 50f) : 0f) * Mathf.Max(1, level));
        yield return AnimateFloat(0f, xpTargetFill, xpFillDuration, v =>
        {
            if (experienceFillImage) experienceFillImage.fillAmount = v;
        });
        yield return AnimateFloat(0f, experience, xpFillDuration, v =>
        {
            if (experienceDetailsTMP) experienceDetailsTMP.text = Mathf.RoundToInt(v) + "/50 XP";
        });
        // Gained XP fill and text (0 -> experienceGained)
        float xpGainTargetFill = Mathf.Clamp01((50f > 0f ? (experienceGained / 50f) : 0f) * Mathf.Max(1, level));
        yield return AnimateFloat(0f, xpGainTargetFill, xpGainFillDuration, v =>
        {
            if (experienceGainedFillImage) experienceGainedFillImage.fillAmount = v;
        });
        yield return AnimateFloat(0f, experienceGained, xpGainFillDuration, v =>
        {
            if (experienceGainTMP) experienceGainTMP.text = "+" + Mathf.RoundToInt(v) + " XP";
        });
        // Level (0 -> level)
        yield return AnimateFloat(0f, Mathf.Max(0, level), levelCountDuration, v =>
        {
            if (levelTMP) levelTMP.text = Mathf.RoundToInt(v).ToString();
        });
        // Ranked points (old -> new)
        if (rankedPointTMP && oldRankedPoint >= 0 && newRankedPoint >= 0)
        {
            yield return AnimateFloat(oldRankedPoint, newRankedPoint, rankedPointsDuration, v =>
            {
                int cur = Mathf.RoundToInt(v);
                rankedPointTMP.text = oldRankedPoint + " > " + cur + " RP";
            });
        }

        _sequenceCoroutine = null;
        _isAnimating = false;
    }

    private void Update()
    {
        if (enableSkipKey && _isAnimating && Input.GetKeyDown(skipKey))
        {
            SkipAnimations();
        }
    }

    public void SkipAnimations()
    {
        if (!_isAnimating) return;
        _skipRequested = true;
        StopAllCoroutines();
        ApplyFinalState();
        _isAnimating = false;
        _sequenceCoroutine = null;
    }

    public void OnSpectatorModeClicked()
    {
        try
        {
            Debug.Log("[GameOverScreen] Spectate button clicked");
            var pr = GameController.instance != null ? GameController.instance.playerReference : null;
            if (pr == null)
            {
                Debug.LogWarning("[GameOverScreen] No local PlayerReference found via GameController.instance");
            }

        }
        catch { }
        Debug.Log("[GameOverScreen] Destroying END instance after spectate click");
        DestroyEndInstance();
    }

    public void UpdateSpectateTargetName(string name)
    {
        try
        {
            if (spectateTargetName == null) return;
            spectateTargetName.SetVariables(new Dictionary<string, string> { { "player", name ?? string.Empty } });
        }
        catch { }
    }

    public void ClearSpectateTargetName()
    {
        try
        {
            if (spectateTargetName == null) return;
            spectateTargetName.SetVariables(new Dictionary<string, string> { { "player", string.Empty } });
        }
        catch { }
    }

    private void ApplyFinalState()
    {
        // Names
        if (characterNameTMP) characterNameTMP.text = _tCharacterName;
        if (spiritNameTMP) spiritNameTMP.text = _tSpiritName;

        // XP
        float xpTargetFill = Mathf.Clamp01((50f > 0f ? (_tExperience / 50f) : 0f) * Mathf.Max(1, _tLevel));
        if (experienceFillImage) experienceFillImage.fillAmount = xpTargetFill;
        if (experienceDetailsTMP) experienceDetailsTMP.text = _tExperience + "/50 XP";

        float xpGainTargetFill = Mathf.Clamp01((50f > 0f ? (_tExperienceGained / 50f) : 0f) * Mathf.Max(1, _tLevel));
        if (experienceGainedFillImage) experienceGainedFillImage.fillAmount = xpGainTargetFill;
        if (experienceGainTMP) experienceGainTMP.text = "+" + _tExperienceGained + " XP";

        // Level
        if (levelTMP) levelTMP.text = Mathf.Max(0, _tLevel).ToString();

        // Stats localized
        if (killsTMP)
        {
            killsTMP.SetVariables(new Dictionary<string, string> { { "number", Mathf.Max(0, _tKills).ToString() } });
            killsTMP.SetText(74);
        }
        if (soulsTMP)
        {
            soulsTMP.SetVariables(new Dictionary<string, string> { { "number", Mathf.Max(0, _tSouls).ToString() } });
            soulsTMP.SetText(73);
        }

        // Ranked points
        if (rankedPointTMP)
        {
            if (_tOldRankedPoint >= 0 && _tNewRankedPoint >= 0)
                rankedPointTMP.text = _tOldRankedPoint + " > " + _tNewRankedPoint + " RP";
            else
                rankedPointTMP.text = string.Empty;
        }
    }

    private void DestroyEndInstance()
    {
        if (_endInstance != null)
        {
            Destroy(_endInstance);
            _endInstance = null;
        }
    }

    private IEnumerator TypeText(TMP_Text label, string fullText, float duration)
    {
        if (!label) yield break;
        if (string.IsNullOrEmpty(fullText) || duration <= 0f)
        {
            label.text = fullText ?? string.Empty;
            yield break;
        }

        label.text = string.Empty;
        float t = 0f;
        int total = fullText.Length;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / duration);
            int count = Mathf.Clamp(Mathf.FloorToInt(total * ratio), 0, total);
            label.text = count > 0 ? fullText.Substring(0, count) : string.Empty;
            yield return null;
        }
        label.text = fullText;
    }

    private IEnumerator AnimateFloat(float start, float end, float duration, System.Action<float> onUpdate)
    {
        if (duration <= 0f)
        {
            onUpdate?.Invoke(end);
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            onUpdate?.Invoke(v);
            yield return null;
        }
        onUpdate?.Invoke(end);
    }

    private IEnumerator AnimateIntLocalized(LocalizedText loc, int textId, int start, int end, float duration)
    {
        if (!loc) yield break;
        if (duration <= 0f)
        {
            loc.SetVariables(new Dictionary<string, string> { { "number", end.ToString() } });
            loc.SetText(textId);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / duration);
            int cur = Mathf.RoundToInt(Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, ratio)));
            loc.SetVariables(new Dictionary<string, string> { { "number", cur.ToString() } });
            loc.SetText(textId);
            yield return null;
        }
        loc.SetVariables(new Dictionary<string, string> { { "number", end.ToString() } });
        loc.SetText(textId);
    }

    // Si la langue active n'a pas de placeholders pour ce textId,
    // on append une "suffix" pour afficher quand même la valeur.
    private void TryLocalizedFallback(LocalizedText loc, int textId, string suffix)
    {
        if (loc == null || GameDataController.instance == null || GameDataController.instance.data == null) return;
        string baseText = GameDataController.instance.GetText(textId);
        if (string.IsNullOrEmpty(baseText)) return;

        // Si le texte ne contient aucun placeholder, on ajoute le suffixe
        if (!baseText.Contains("{"))
        {
            var label = loc.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = baseText + suffix;
            }
        }
    }
}

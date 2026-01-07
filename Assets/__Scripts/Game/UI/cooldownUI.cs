using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game;

public class cooldownUI : MonoBehaviour
{

    public static cooldownUI instance;

    public Image mainAvatarImg;
    public Image secondAvatarImg;
    public Image secondSideAvatarImg;

    public GameObject secondAllSpellsEffectReadyDirector;
    public PlayableDirector transformationEffectDirector;
    [SerializeField] private TextMeshProUGUI Qcd;

    [SerializeField] private Image Qimage;
    [SerializeField] private bool Qstatus;
    [SerializeField] private Image fillQ;

    [SerializeField] private TextMeshProUGUI Zcd;
    [SerializeField] private Image Zimage;
    [SerializeField] private bool Zstatus;
    [SerializeField] private Image fillZ;

    [SerializeField] private TextMeshProUGUI Ecd;
    [SerializeField] private Image Eimage;
    [SerializeField] private bool Estatus;
    [SerializeField] private Image fillE;

    [SerializeField] private TextMeshProUGUI Rcd;
    [SerializeField] private Image Rimage;
    [SerializeField] private bool Rstatus;
    [SerializeField] private Image fillR;

    [SerializeField] private TextMeshProUGUI localPlayerPoints;

    [SerializeField] private TextMeshProUGUI localPlayerHealthText;
    [SerializeField] public TextMeshProUGUI localPlayerShieldText;

    [SerializeField] public TextMeshProUGUI localSoulTMP;

    [SerializeField] private Slider localPlayerHeath;
    [SerializeField] public Image localPlayerShield;
    public Slider reincarnationSlider;

    [SerializeField] private Animator cursor;

    [SerializeField] private GameObject worldMapObject;
    [SerializeField] private Camera worldMapCamera;

    [SerializeField] private GameObject rankingObject;

    public Transform killResumeContainer;
    public GameObject killResumePrefab;

    public Transform damageContainer;
    public GameObject damagePrefab;

    public PlayableDirector Spell1Indicator;
    public PlayableDirector Spell2Indicator;
    public PlayableDirector Spell3Indicator;
    public PlayableDirector Spell4Indicator;
    public PlayableAsset SpellAppearIndicator;
    public PlayableAsset SpellDisappearIndicator;

    /// <summary>
    /// Variable permettant d'avertir au joueur local qu'il est stun et combien de temps il lui reste.
    /// </summary>
    [SerializeField] private TextMeshProUGUI stateStatusMsg;
    [SerializeField] private Image stateStatusImg;
    [SerializeField] private GameObject stateStatus;

    public FeedbackEnemy feedbackEnemy;

    public ReBinder reBinder;

    public SelectionInGame selectionInGame;

    [SerializeField] private Animator hittedEffectAnimator;
    [SerializeField] private Animator soulEffectAnimator;


    [SerializeField] private GameObject helperGameObject;

    public PlayableDirector interactiveDirector;

    public DamageDirectionUI damageDirectionUI;

    private void OnEnable()
    {
        InputManager.inputActions.Player.Helper.performed += OnHelperToggle;
#if !UNITY_SERVER
        InputManager.inputActions.Player.Worldmap.performed += WorldmapAction;
        InputManager.inputActions.Player.Ranking.performed += RankingAction;
#endif
    }

    private void OnDisable()
    {
        InputManager.inputActions.Player.Helper.performed -= OnHelperToggle;
#if !UNITY_SERVER
        try { InputManager.inputActions.Player.Worldmap.performed -= WorldmapAction; } catch { }
        try { InputManager.inputActions.Player.Ranking.performed -= RankingAction; } catch { }
#endif
    }

    private void OnHelperToggle(InputAction.CallbackContext context)
    {
        if (helperGameObject != null)
        {
            helperGameObject.SetActive(!helperGameObject.activeSelf);
        }
    }


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        feedbackEnemy = GetComponent<FeedbackEnemy>();
    }


#if !UNITY_SERVER
    private void Start()
    {
        reincarnationSlider.maxValue = reincarnationSlider.value = 100;
    }

#endif

    private void WorldmapAction(InputAction.CallbackContext context)
    {
        WorldMapPopup();
    }


    /// <summary>
    /// Utilis� dans le bouton close du worldmap UI
    /// </summary>
    public void WorldMapPopup()
    {
        if (worldMapObject != null)
        {
            worldMapObject.SetActive(!worldMapObject.activeSelf);
            worldMapCamera.gameObject.SetActive(!worldMapCamera.gameObject.activeSelf);
        }
    }

    private void RankingAction(InputAction.CallbackContext context)
    {
        RankingPopup();
    }

    /// <summary>
    /// Utilis� dans le bouton close du ranking UI
    /// </summary>
    public void RankingPopup()
    {
        if (rankingObject != null)
        {
            rankingObject.SetActive(!rankingObject.activeSelf);
        }
    }

    public void UpdateIt()
    {
        // GameController doit s'�tre termin� pour pouvoir d�clencher ce Script.
        if (GameController.instance.classes == null)
            return;
        Qimage.sprite = GameController.instance.playerReference.PlayerReincarnation.IsReincarnation ? Resources.Load<Sprite>(GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).GetSpells()[0].icon) : Resources.Load<Sprite>(GameDataController.instance.GetCharacter(PlayerData.player.data.classId).GetSpells()[0].icon);
        Zimage.sprite = GameController.instance.playerReference.PlayerReincarnation.IsReincarnation ? Resources.Load<Sprite>(GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).GetSpells()[1].icon) : Resources.Load<Sprite>(GameDataController.instance.GetCharacter(PlayerData.player.data.classId).GetSpells()[1].icon);
        Eimage.sprite = GameController.instance.playerReference.PlayerReincarnation.IsReincarnation ? Resources.Load<Sprite>(GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).GetSpells()[2].icon) : Resources.Load<Sprite>(GameDataController.instance.GetCharacter(PlayerData.player.data.classId).GetSpells()[2].icon);
        Rimage.sprite = GameController.instance.playerReference.PlayerReincarnation.IsReincarnation ? Resources.Load<Sprite>(GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).GetSpells()[3].icon) : Resources.Load<Sprite>(GameDataController.instance.GetCharacter(PlayerData.player.data.classId).GetSpells()[3].icon);
        UpdateUiInformation();

    }


#if !UNITY_SERVER
    private void Update()
    {
        // Q
        if (GameController.instance.obj == null)
            return;

        if ((GameController.instance.classes.warriorSpell1CD != 0.0f && !GameController.instance.playerReference.PlayerReincarnation.IsReincarnation) || (GameController.instance.classes.spiritSpell1CD != 0.0f && GameController.instance.playerReference.PlayerReincarnation.IsReincarnation))
        {
            float currentCooldown;
            if (!GameController.instance.playerReference.PlayerReincarnation.IsReincarnation)
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.warriorSpell1CD * 10.0f) * 0.1f;
            }
            else
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.spiritSpell1CD * 10.0f) * 0.1f;
            }

            Qcd.text = Mathf.FloorToInt(currentCooldown) + "";

            float finalCooldown = GetReferenceCooldown(0);

            fillQ.fillAmount = finalCooldown > 0f ? currentCooldown / finalCooldown : 0f;

            if (Qstatus)
            {
                Qstatus = false;
                Qimage.color = new Color(1, 1, 1, .5f);
                Qcd.gameObject.SetActive(true);
                fillQ.gameObject.SetActive(true);
            }
        }
        else
        {
            if (!Qstatus)
            {
                Qstatus = true;
                Qimage.color = new Color(1, 1, 1, 1);
                Qcd.gameObject.SetActive(false);
                fillQ.gameObject.SetActive(false);
            }
        }

        // Z
        if ((GameController.instance.classes.warriorSpell2CD != 0.0f && !GameController.instance.playerReference.PlayerReincarnation.IsReincarnation) || (GameController.instance.classes.spiritSpell2CD != 0.0f && GameController.instance.playerReference.PlayerReincarnation.IsReincarnation))
        {
            float currentCooldown;
            if (!GameController.instance.playerReference.PlayerReincarnation.IsReincarnation)
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.warriorSpell2CD * 10.0f) * 0.1f;
            }
            else
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.spiritSpell2CD * 10.0f) * 0.1f;
            }
            Zcd.text = Mathf.FloorToInt(currentCooldown) + "";

            float finalCooldown = GetReferenceCooldown(1);

            fillZ.fillAmount = finalCooldown > 0f ? currentCooldown / finalCooldown : 0f;

            if (Zstatus)
            {
                Zstatus = false;
                Zimage.color = new Color(1, 1, 1, .5f);
                Zcd.gameObject.SetActive(true);
                fillZ.gameObject.SetActive(true);
            }
        }
        else
        {
            if (!Zstatus)
            {
                Zstatus = true;
                Zimage.color = new Color(1, 1, 1, 1);
                Zcd.gameObject.SetActive(false);
                fillZ.gameObject.SetActive(false);
            }
        }

        // E
        if ((GameController.instance.classes.warriorSpell3CD != 0.0f && !GameController.instance.playerReference.PlayerReincarnation.IsReincarnation) || (GameController.instance.classes.spiritSpell3CD != 0.0f && GameController.instance.playerReference.PlayerReincarnation.IsReincarnation))
        {
            float currentCooldown;
            if (!GameController.instance.playerReference.PlayerReincarnation.IsReincarnation)
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.warriorSpell3CD * 10.0f) * 0.1f;
            }
            else
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.spiritSpell3CD * 10.0f) * 0.1f;
            }
            Ecd.text = Mathf.FloorToInt(currentCooldown) + "";

            float finalCooldown = GetReferenceCooldown(2);

            fillE.fillAmount = finalCooldown > 0f ? currentCooldown / finalCooldown : 0f;


            if (Estatus)
            {
                Estatus = false;
                Eimage.color = new Color(1, 1, 1, .5f);
                Ecd.gameObject.SetActive(true);
                fillE.gameObject.SetActive(true);
            }
        }
        else
        {
            if (!Estatus)
            {
                Estatus = true;
                Eimage.color = new Color(1, 1, 1, 1);
                Ecd.gameObject.SetActive(false);
                fillE.gameObject.SetActive(false);
            }
        }
        // R
        if ((GameController.instance.classes.warriorSpell4CD != 0.0f && !GameController.instance.playerReference.PlayerReincarnation.IsReincarnation) || (GameController.instance.classes.spiritSpell4CD != 0.0f && GameController.instance.playerReference.PlayerReincarnation.IsReincarnation))
        {
            float currentCooldown;
            if (!GameController.instance.playerReference.PlayerReincarnation.IsReincarnation)
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.warriorSpell4CD * 10.0f) * 0.1f;
            }
            else
            {
                currentCooldown = Mathf.Round(GameController.instance.classes.spiritSpell4CD * 10.0f) * 0.1f;
            }
            float finalCooldown = GetReferenceCooldown(3);

            fillR.fillAmount = finalCooldown > 0f ? currentCooldown / finalCooldown : 0f;


            Rcd.text = Mathf.FloorToInt(currentCooldown) + "";

            if (Rstatus)
            {
                Rstatus = false;
                Rimage.color = new Color(1, 1, 1, .5f);
                Rcd.gameObject.SetActive(true);
                fillR.gameObject.SetActive(true);
            }
        }
        else
        {
            if (!Rstatus)
            {
                Rstatus = true;
                Rimage.color = new Color(1, 1, 1, 1);
                Rcd.gameObject.SetActive(false);
                fillR.gameObject.SetActive(false);
            }
        }

        if (GameController.instance.stats.ReincarnationPower < 100)
        {
            reincarnationSlider.value = GameController.instance.stats.ReincarnationPower;
        }
        else
        {
            if (reincarnationSlider.value != 100) reincarnationSlider.value = 100;
            if (reincarnationSlider.value != 100) reincarnationSlider.value = GameController.instance.stats.ReincarnationPower;
        }
        if (worldMapObject.activeSelf)
        {
            worldMapCamera.Render();
        }

    }

    private float GetReferenceCooldown(int spellIndex)
    {
        var zone = (Run.instance != null && Run.instance.CPUcontroller != null) ? Run.instance.CPUcontroller.zone : null;
        bool isSurvivorMode = zone != null && zone.serverMode == GameMode.Survivor;
        bool isStreamerMode = zone != null && zone.serverMode == GameMode.Streamer;

        var spellData = GameController.instance.classes.spells[spellIndex];
        float baseCooldown = spellData.spellCooldown;
        float cooldown = baseCooldown;

        if (isSurvivorMode || isStreamerMode)
        {
        }
        else
        {
            cooldown -= GameController.instance.stats.playerStatData.soul * 0.5f;
        }

        cooldown = Mathf.Max(0f, cooldown);

        if (isSurvivorMode)
        {
            return cooldown;
        }

        return Mathf.Max(spellData.spellMinCooldown, cooldown);
    }
#endif
    public void UpdateUiInformation()
    {
        if (GameController.instance.stats)
        {
            if (GameController.instance.stats.playerStatData.maxHealth < GameController.instance.stats.playerStatData.health)
            {
                GameController.instance.stats.playerStatData.maxHealth = GameController.instance.stats.playerStatData.health;
            }
            localPlayerHeath.maxValue = GameController.instance.stats.playerStatData.maxHealth;
            localPlayerHeath.value = GameController.instance.stats.playerStatData.health;
            localPlayerHealthText.text = Mathf.Round(GameController.instance.stats.playerStatData.health).ToString() + " / " + Mathf.Round(GameController.instance.stats.playerStatData.maxHealth);
            localPlayerShieldText.text = Mathf.Round(GameController.instance.stats.playerStatData.shield).ToString();
            localPlayerShield.fillAmount = GameController.instance.stats.playerStatData.shield / 100f;
            localSoulTMP.text = GameController.instance.stats.playerStatData.soul.ToString();

            bool isReincarnation = GameController.instance.playerReference.PlayerReincarnation.IsReincarnation;
            mainAvatarImg.sprite = Resources.Load<Sprite>(isReincarnation ? GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).icon : GameDataController.instance.GetCharacter(PlayerData.player.data.classId).icon);
            Sprite secondImage = Resources.Load<Sprite>(isReincarnation ? GameDataController.instance.GetCharacter(PlayerData.player.data.classId).icon : GameDataController.instance.GetReincarnation(PlayerData.player.data.reId).icon);
            secondAvatarImg.sprite = secondImage;
            secondSideAvatarImg.sprite = secondImage;

            var playerClasses = GameController.instance.playerReference.playerClasses;
            PlayableAsset[] spellIndicators = new PlayableAsset[] {
            (isReincarnation ? playerClasses.warriorSpell1CD : playerClasses.spiritSpell1CD) <= 0 ? SpellAppearIndicator : SpellDisappearIndicator,
            (isReincarnation ? playerClasses.warriorSpell2CD : playerClasses.spiritSpell2CD) <= 0 ? SpellAppearIndicator : SpellDisappearIndicator,
            (isReincarnation ? playerClasses.warriorSpell3CD : playerClasses.spiritSpell3CD) <= 0 ? SpellAppearIndicator : SpellDisappearIndicator,
            (isReincarnation ? playerClasses.warriorSpell4CD : playerClasses.spiritSpell4CD) <= 0 ? SpellAppearIndicator : SpellDisappearIndicator
        };

            PlayableDirector[] spellDirectors = { Spell1Indicator, Spell2Indicator, Spell3Indicator, Spell4Indicator };

            for (int i = 0; i < spellDirectors.Length; i++)
            {
                if (spellDirectors[i].playableAsset != spellIndicators[i])
                {
                    spellDirectors[i].Play(spellIndicators[i]);
                }
            }

            if (spellIndicators.All(indicator => indicator == SpellAppearIndicator))
            {
                secondAllSpellsEffectReadyDirector.SetActive(true);
            }
            else
            {
                secondAllSpellsEffectReadyDirector.SetActive(false);
            }
        }
    }


    public void PlayHitEffect()
    {
        hittedEffectAnimator.SetTrigger("hit");
    }

    public void PlaySoulEffect()
    {
        soulEffectAnimator.SetTrigger("soul");
    }

    public void ReduceReincarnation()
    {
        reincarnationSlider.value -= 25;
    }

    public void SetStateMsg(double value)
    {
        stateStatusImg.fillAmount = (float)(value / 10);
        stateStatusMsg.text = value + " s";
    }

    public void ChangeStateMsg(bool boolo)
    {
        stateStatus.gameObject.SetActive(boolo);
    }

    public void PlayCursorHitAnimation()
    {
        cursor.SetTrigger("hit");
    }

    public void UpdateEnemyIndicator(PlayerReference playerRef)
    {
        feedbackEnemy.UpdateUiInformation(playerRef);
    }
}



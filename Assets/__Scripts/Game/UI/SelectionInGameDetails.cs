using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelectionInGameDetails : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI characterNameTMP;
    [SerializeField] Image characterImg;


    [SerializeField] TextMeshProUGUI spellQ;
    [SerializeField] TextMeshProUGUI descQ;
    [SerializeField] Image iconQ;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellZ;
    [SerializeField] TextMeshProUGUI descZ;
    [SerializeField] Image iconZ;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellE;
    [SerializeField] TextMeshProUGUI descE;
    [SerializeField] Image iconE;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellR;
    [SerializeField] TextMeshProUGUI descR;
    [SerializeField] Image iconR;

    [SerializeField] private Image[] difficultyStars;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite starEmpty;

    [SerializeField] private TextMeshProUGUI characterTypeTMP;


    [Space(50)]

    [SerializeField] TextMeshProUGUI characterNameHelperTMP;
    [SerializeField] Image characterHelperImg;


    [SerializeField] TextMeshProUGUI spellQHelper;
    [SerializeField] TextMeshProUGUI descQHelper;
    [SerializeField] Image iconQHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellZHelper;
    [SerializeField] TextMeshProUGUI descZHelper;
    [SerializeField] Image iconZHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellEHelper;
    [SerializeField] TextMeshProUGUI descEHelper;
    [SerializeField] Image iconEHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spellRHelper;
    [SerializeField] TextMeshProUGUI descRHelper;
    [SerializeField] Image iconRHelper;

    [SerializeField] private Image[] difficultyStarsHelper;
    [SerializeField] private Sprite starFilledHelper;
    [SerializeField] private Sprite starEmptyHelper;

    [SerializeField] private TextMeshProUGUI characterTypeTMPHelper;

    [Space(50)]

    [SerializeField] TextMeshProUGUI spiritNameHelperTMP;
    [SerializeField] Image spiritHelperImg;


    [SerializeField] TextMeshProUGUI spiritspellQHelper;
    [SerializeField] TextMeshProUGUI spiritdescQHelper;
    [SerializeField] Image spiriticonQHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spiritspellZHelper;
    [SerializeField] TextMeshProUGUI spiritdescZHelper;
    [SerializeField] Image spiriticonZHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spiritspellEHelper;
    [SerializeField] TextMeshProUGUI spiritdescEHelper;
    [SerializeField] Image spiriticonEHelper;

    [Space(5)]

    [SerializeField] TextMeshProUGUI spiritspellRHelper;
    [SerializeField] TextMeshProUGUI spiritdescRHelper;
    [SerializeField] Image spiriticonRHelper;

    [SerializeField] private Image[] spiritdifficultyStarsHelper;
    [SerializeField] private Sprite spiritstarFilledHelper;
    [SerializeField] private Sprite spiritstarEmptyHelper;

    [SerializeField] private TextMeshProUGUI spiritcharacterTypeTMPHelper;

    private static Sprite LoadSpriteSafe(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Resources.Load<Sprite>(path);
    }

    private void FillSpellSet(
        IList<Json.Spell> spells,
        TextMeshProUGUI qName, TextMeshProUGUI qDesc, Image qIcon,
        TextMeshProUGUI zName, TextMeshProUGUI zDesc, Image zIcon,
        TextMeshProUGUI eName, TextMeshProUGUI eDesc, Image eIcon,
        TextMeshProUGUI rName, TextMeshProUGUI rDesc, Image rIcon)
    {
        // Sécurité : certaines fiches pourraient ne pas avoir 4 sorts
        string SafeName(int i) => (spells != null && spells.Count > i && spells[i] != null) ? spells[i].name : "";
        string SafeDesc(int i) => (spells != null && spells.Count > i && spells[i] != null) ? spells[i].description : "";
        Sprite SafeIcon(int i) => (spells != null && spells.Count > i && spells[i] != null) ? LoadSpriteSafe(spells[i].icon) : null;

        if (qName) qName.text = SafeName(0);
        if (qDesc) qDesc.text = SafeDesc(0);
        if (qIcon) qIcon.sprite = SafeIcon(0);

        if (zName) zName.text = SafeName(1);
        if (zDesc) zDesc.text = SafeDesc(1);
        if (zIcon) zIcon.sprite = SafeIcon(1);

        if (eName) eName.text = SafeName(2);
        if (eDesc) eDesc.text = SafeDesc(2);
        if (eIcon) eIcon.sprite = SafeIcon(2);

        if (rName) rName.text = SafeName(3);
        if (rDesc) rDesc.text = SafeDesc(3);
        if (rIcon) rIcon.sprite = SafeIcon(3);
    }

    private void FillDifficulty(
        int difficulty,
        Image[] stars,
        Sprite filled,
        Sprite empty)
    {
        if (stars == null) return;
        for (int i = 0; i < stars.Length; i++)
        {
            if (!stars[i]) continue;
            stars[i].sprite = (i < difficulty) ? filled : empty;
        }
    }

    // --- Remplissage Personnage (haut + Helper) ---
    private void FillCharacterUI(
        string name,
        string fullIconPath,
        string type,
        IList<Json.Spell> spells,
        // bloc principal
        TextMeshProUGUI nameTMP, Image portrait, TextMeshProUGUI typeTMP,
        TextMeshProUGUI qName, TextMeshProUGUI qDesc, Image qIcon,
        TextMeshProUGUI zName, TextMeshProUGUI zDesc, Image zIcon,
        TextMeshProUGUI eName, TextMeshProUGUI eDesc, Image eIcon,
        TextMeshProUGUI rName, TextMeshProUGUI rDesc, Image rIcon,
        Image[] stars, Sprite starOn, Sprite starOff, int difficulty,
        // bloc helper
        TextMeshProUGUI nameTMPHelper, Image portraitHelper, TextMeshProUGUI typeTMPHelper,
        TextMeshProUGUI qNameH, TextMeshProUGUI qDescH, Image qIconH,
        TextMeshProUGUI zNameH, TextMeshProUGUI zDescH, Image zIconH,
        TextMeshProUGUI eNameH, TextMeshProUGUI eDescH, Image eIconH,
        TextMeshProUGUI rNameH, TextMeshProUGUI rDescH, Image rIconH,
        Image[] starsH, Sprite starOnH, Sprite starOffH, int difficultyH)
    {
        // Bloc principal
        if (nameTMP) nameTMP.text = name;
        if (portrait) portrait.sprite = LoadSpriteSafe(fullIconPath);
        if (typeTMP) typeTMP.text = type;
        FillSpellSet(spells, qName, qDesc, qIcon, zName, zDesc, zIcon, eName, eDesc, eIcon, rName, rDesc, rIcon);
        FillDifficulty(difficulty, stars, starOn, starOff);

        // Bloc Helper (miroir)
        if (nameTMPHelper) nameTMPHelper.text = name;
        if (portraitHelper) portraitHelper.sprite = LoadSpriteSafe(fullIconPath);
        if (typeTMPHelper) typeTMPHelper.text = type;
        FillSpellSet(spells, qNameH, qDescH, qIconH, zNameH, zDescH, zIconH, eNameH, eDescH, eIconH, rNameH, rDescH, rIconH);
        FillDifficulty(difficultyH, starsH, starOnH, starOffH);
    }

    // --- Remplissage Esprit/Réincarnation (haut + SpiritHelper) ---
    private void FillSpiritUI(
        string name,
        string fullIconPath,
        string type,
        IList<Json.Spell> spells,
        // bloc principal (réutilise les champs "character" du haut comme dans ton FillSpiritInformation)
        TextMeshProUGUI nameTMP, Image portrait, TextMeshProUGUI typeTMP,
        TextMeshProUGUI qName, TextMeshProUGUI qDesc, Image qIcon,
        TextMeshProUGUI zName, TextMeshProUGUI zDesc, Image zIcon,
        TextMeshProUGUI eName, TextMeshProUGUI eDesc, Image eIcon,
        TextMeshProUGUI rName, TextMeshProUGUI rDesc, Image rIcon,
        Image[] stars, Sprite starOn, Sprite starOff, int difficulty,
        // bloc SpiritHelper
        TextMeshProUGUI nameTMPHelper, Image portraitHelper, TextMeshProUGUI typeTMPHelper,
        TextMeshProUGUI qNameH, TextMeshProUGUI qDescH, Image qIconH,
        TextMeshProUGUI zNameH, TextMeshProUGUI zDescH, Image zIconH,
        TextMeshProUGUI eNameH, TextMeshProUGUI eDescH, Image eIconH,
        TextMeshProUGUI rNameH, TextMeshProUGUI rDescH, Image rIconH,
        Image[] starsH, Sprite starOnH, Sprite starOffH, int difficultyH)
    {
        // Bloc principal (affichage du spirit en haut, comme tu le faisais déjà)
        if (nameTMP) nameTMP.text = name;
        if (portrait) portrait.sprite = LoadSpriteSafe(fullIconPath);
        if (typeTMP) typeTMP.text = type;
        FillSpellSet(spells, qName, qDesc, qIcon, zName, zDesc, zIcon, eName, eDesc, eIcon, rName, rDesc, rIcon);
        FillDifficulty(difficulty, stars, starOn, starOff);

        // Bloc SpiritHelper (miroir)
        if (nameTMPHelper) nameTMPHelper.text = name;
        if (portraitHelper) portraitHelper.sprite = LoadSpriteSafe(fullIconPath);
        if (typeTMPHelper) typeTMPHelper.text = type;
        FillSpellSet(spells, qNameH, qDescH, qIconH, zNameH, zDescH, zIconH, eNameH, eDescH, eIconH, rNameH, rDescH, rIconH);
        FillDifficulty(difficultyH, starsH, starOnH, starOffH);
    }

    // =====================
    // == Méthodes publiées ==
    // =====================

    public void FillInformation(Json.Character character)
    {
        var spells = character.GetSpells();

        FillCharacterUI(
            character.name,
            character.fullIcon,
            character.characterType,
            spells,
            // haut
            characterNameTMP, characterImg, characterTypeTMP,
            spellQ, descQ, iconQ,
            spellZ, descZ, iconZ,
            spellE, descE, iconE,
            spellR, descR, iconR,
            difficultyStars, starFilled, starEmpty, character.difficulty,
            // helper personnage
            characterNameHelperTMP, characterHelperImg, characterTypeTMPHelper,
            spellQHelper, descQHelper, iconQHelper,
            spellZHelper, descZHelper, iconZHelper,
            spellEHelper, descEHelper, iconEHelper,
            spellRHelper, descRHelper, iconRHelper,
            difficultyStarsHelper, starFilledHelper, starEmptyHelper, character.difficulty
        );
    }

    public void FillSpiritInformation(Json.Reincarnation spirit)
    {
        var spells = spirit.GetSpells();

        FillSpiritUI(
            spirit.name,
            spirit.fullIcon,
            spirit.characterType,
            spells,
            // haut (tu affiches le spirit dans les mêmes champs "character" du haut)
            characterNameTMP, characterImg, characterTypeTMP,
            spellQ, descQ, iconQ,
            spellZ, descZ, iconZ,
            spellE, descE, iconE,
            spellR, descR, iconR,
            difficultyStars, starFilled, starEmpty, spirit.difficulty,
            // helper esprit
            spiritNameHelperTMP, spiritHelperImg, spiritcharacterTypeTMPHelper,
            spiritspellQHelper, spiritdescQHelper, spiriticonQHelper,
            spiritspellZHelper, spiritdescZHelper, spiriticonZHelper,
            spiritspellEHelper, spiritdescEHelper, spiriticonEHelper,
            spiritspellRHelper, spiritdescRHelper, spiriticonRHelper,
            spiritdifficultyStarsHelper, spiritstarFilledHelper, spiritstarEmptyHelper, spirit.difficulty
        );
    }


}

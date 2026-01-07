using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionInGame : MonoBehaviour
{

    [SerializeField] private Transform containerCharacters;
    [SerializeField] private GameObject characterPrefab;

    [SerializeField] private Transform containerSpirit;
    [SerializeField] private GameObject spiritPrefab;

    CPUcontroller cpucontroller;

    private List<CharacterSelectionItem> characters;
    private List<CharacterSelectionItem> spirits;
    private HashSet<int> allowedReincarnations = new HashSet<int> { 0 };

    private SelectionInGameDetails selectionInGameDetails;
    [SerializeField] GameObject detailsObj;

    [HideInInspector] public CanvasGroup selectionCanvasGroup;
    [SerializeField] public GameObject nextButton;

#if !UNITY_SERVER
    void Start()
    {
        selectionCanvasGroup.alpha = 0;

        cpucontroller = Run.instance.CPUcontroller;
        selectionInGameDetails = GetComponent<SelectionInGameDetails>();
        characters = new List<CharacterSelectionItem>();
        spirits = new List<CharacterSelectionItem>();

        foreach (var character in GameDataController.instance.data.characters)
        {
            GameObject charObj = Instantiate(characterPrefab, containerCharacters);
            CharacterSelectionItem charItem = charObj.GetComponent<CharacterSelectionItem>();
            charItem.UpdateChar(character);
            characters.Add(charItem);
        }

        foreach (var reincarnation in GameDataController.instance.data.reincarnations)
        {
            GameObject spiritObj = Instantiate(spiritPrefab, containerSpirit);
            CharacterSelectionItem spiritItem = spiritObj.GetComponent<CharacterSelectionItem>();
            spiritItem.UpdateSpirit(reincarnation);
            spirits.Add(spiritItem);
        }

    }
#endif

    public void SelectClass(int classId)
    {
        PlayerData.player.data.classId = classId;
        foreach (CharacterSelectionItem characterSelection in characters)
        {
            if (characterSelection.character.id != classId)
            {
                characterSelection.buttonTransition.ApplyChangeToElements(ButtonTransition.ChangeEnum.NORMAL);
            }
            else
            {
                characterSelection.buttonTransition.ApplyChangeToElements(ButtonTransition.ChangeEnum.SELECTED);
                selectionInGameDetails.FillInformation(characterSelection.character);
                detailsObj.SetActive(true);
            }
        }
        SoundManager.Instance.Play2D("special-interaction");
        nextButton.SetActive(true);
    }

    public void SelectSpirit(int spiritId)
    {
        PlayerData.player.data.reId = spiritId;
        foreach (CharacterSelectionItem spiritSelection in spirits)
        {
            if (spiritSelection.spirit.id != spiritId)
            {
                if (spiritSelection.buttonTransition.oldState != ButtonTransition.ChangeEnum.DISABLED)
                {
                    spiritSelection.buttonTransition.ApplyChangeToElements(ButtonTransition.ChangeEnum.NORMAL);
                }
            }
            else
            {
                selectionInGameDetails.FillSpiritInformation(spiritSelection.spirit);
                spiritSelection.buttonTransition.ApplyChangeToElements(ButtonTransition.ChangeEnum.SELECTED);
            }
        }
        SoundManager.Instance.Play2D("special-interaction");
    }

    public void SetAllowedReincarnations(int[] ids)
    {
        Debug.Log(allowedReincarnations);
        allowedReincarnations = new HashSet<int>(ids);
        UpdateSpiritAvailability();
        SelectClass(PlayerPrefs.GetInt("lastClassId"));
        Cursor.lockState = CursorLockMode.None;
    }

    private void UpdateSpiritAvailability()
    {
        if (spirits == null) return;
        foreach (var spiritSelection in spirits)
        {
            Debug.Log("Set availability for " + spiritSelection.spirit.name);
            bool allowed = allowedReincarnations.Contains(spiritSelection.SpiritId);
            spiritSelection.SetAvailability(allowed);
        }
    }

    public void GoToSpiritSelection()
    {
        SelectSpirit(PlayerPrefs.GetInt("lastSpiritId"));
    }

    public void ConfirmSelection()
    {
        Debug.Log(PlayerData.player.data.reId + " et " + PlayerData.player.data.classId + " sélectionnés");
        PlayerPrefs.SetInt("lastClassId", PlayerData.player.data.classId);
        PlayerPrefs.SetInt("lastSpiritId", PlayerData.player.data.reId);
        PlayerPrefs.Save();
        cpucontroller.SelectClassInGameSelection();
        Cursor.lockState = CursorLockMode.Locked;
        SoundManager.Instance.Play2D("special-button");
    }

}

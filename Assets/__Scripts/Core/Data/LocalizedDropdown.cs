using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizedDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    [SerializeField] private string playerPrefKey;

    // Liste des textId à afficher dans le dropdown (dans le même ordre que les options)
    public List<int> optionTextIds = new List<int>();

    private void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        GameDataController.OnLanguageChanged += UpdateLocalizedOptions;
        UpdateLocalizedOptions();
    }

    private void OnDisable()
    {
        GameDataController.OnLanguageChanged -= UpdateLocalizedOptions;
    }

    public void UpdateLocalizedOptions()
    {
        if (GameDataController.instance == null || GameDataController.instance.data == null) return;
        if (dropdown == null || optionTextIds.Count == 0) return;

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (int id in optionTextIds)
        {
            string label = GameDataController.instance.GetText(id);
            options.Add(new TMP_Dropdown.OptionData(label));
        }

        dropdown.options = options;
        // dropdown.captionText.text = options[dropdown.value].text;
        dropdown.SetValueWithoutNotify(int.Parse(PlayerPrefs.GetString(playerPrefKey, "0")));
    }
}
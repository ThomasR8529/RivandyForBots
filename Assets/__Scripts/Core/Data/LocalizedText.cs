using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System;

public class LocalizedText : MonoBehaviour
{
    public int textId;
    public Dictionary<string, string> variables = new Dictionary<string, string>();

    [SerializeField] private TextMeshProUGUI textComponent;

    public bool changeOnEnable = true;

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        GameDataController.OnLanguageChanged += SetText;
        if (changeOnEnable)
        {
            SetText();
        }
    }

    private void SetText()
    {
        SetText(-1);
    }

    private void OnDestroy()
    {
        GameDataController.OnLanguageChanged -= SetText;
    }

    public void SetText(int newTextId)
    {
        if (GameDataController.instance == null || GameDataController.instance.data == null)
        {
            textComponent.text = "[No data]";
            return;
        }

        string baseText = GameDataController.instance.GetText(newTextId == -1 ? textId : newTextId);
        foreach (var kvp in variables)
        {
            baseText = baseText.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        if (textComponent == null)
        {
            Debug.Log(gameObject.name + " a un problème pour traduire: textmeshpro absente");
        }
        else
        {
            textComponent.text = baseText;
        }
    }

    // Tu peux appeler cette méthode si tu veux mettre à jour les variables et forcer un refresh
    public void SetVariables(Dictionary<string, string> newVars)
    {
        variables = newVars;
        SetText();
    }
}
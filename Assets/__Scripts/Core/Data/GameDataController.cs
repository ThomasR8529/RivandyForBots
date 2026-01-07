using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class GameDataController : MonoBehaviour
{

    public static GameDataController instance;
    public GameData data;

    public string language = "0";
    public string token = "";
    public ulong steamId;

    public static Action OnLanguageChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        string savedLang = PlayerPrefs.GetString("language", "0");
        if (string.IsNullOrEmpty(savedLang) || savedLang == "-1")
        {
            savedLang = "0"; // fallback to English
            PlayerPrefs.SetString("language", savedLang);
            PlayerPrefs.Save();
        }
        language = savedLang;
        LoadLanguage(language);
        DontDestroyOnLoad(gameObject);
    }

    public void SetLanguage(string newLanguage)
    {
        if (string.IsNullOrEmpty(newLanguage) || newLanguage == "-1")
        {
            newLanguage = "0"; // ensure a valid language
        }

        if (language != newLanguage)
        {
            language = newLanguage;
            PlayerPrefs.SetString("language", newLanguage);
            PlayerPrefs.Save();
            LoadLanguage(language);
            OnLanguageChanged?.Invoke();
        }
    }

    private void LoadLanguage(string lang)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "TextTable", lang + ".json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Missing language file at path: {filePath}, falling back to English");
            lang = "0";
            filePath = Path.Combine(Application.streamingAssetsPath, "TextTable", lang + ".json");
            PlayerPrefs.SetString("language", lang);
            PlayerPrefs.Save();
        }

        if (File.Exists(filePath))
        {
            data = JsonUtility.FromJson<GameData>(File.ReadAllText(filePath));
            Debug.Log("Language loaded: " + filePath);
        }
        else
        {
            Debug.LogError($"Missing default language file at path: {filePath}");
        }
    }

    public Character GetCharacter(string characterName)
    {
        return data.characters.Find(c => c.name == characterName);
    }
    public Character GetCharacter(int characterId)
    {
        return data.characters.Find(c => c.id == characterId);
    }

    public Spell GetSpell(int spellId)
    {
        return data.spells.Find(c => c.id == spellId);
    }

    public Spell GetSpell(string spellName)
    {
        return data.spells.Find(c => c.name == spellName);
    }

    public Monster GetMonster(string monsterName)
    {
        return data.monsters.Find(c => c.name == monsterName);
    }
    public Reincarnation GetReincarnation(string reName)
    {
        return data.reincarnations.Find(c => c.name == reName);
    }

    public Reincarnation GetReincarnation(int reId)
    {
        return data.reincarnations.Find(c => c.id == reId);
    }

    public ShopObject GetGameObject(string objectName)
    {
        return data.objects.Find(c => c.name == objectName);
    }

    public string GetText(int id)
    {
        if (id >= 0 && id < data.dialogs.Count)
            return data.dialogs[id].text;
        return $"[MissingText:{id}]";
    }

    public ShopItemData? GetItemInformation(int shopId)
    {
        var item = data.objects.Find(c => c.id == shopId);
        if (item != null)
        {
            return new ShopItemData
            {
                ShopId = shopId,
                Name = item.name,
                Description = item.description,
                Icon = item.icon
            };
        }
        else
        {
            Debug.LogError($"Item with ShopId {shopId} not found.");
            return null;
        }
    }



}

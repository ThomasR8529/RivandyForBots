
using System;
using System.Collections.Generic;
using Json;
using System.Linq;
using UnityEngine;
public static class TextImporter
{
    public static string GetDialog(int id)
    {
        Dialogs dialog = GameDataController.instance.data.dialogs.FirstOrDefault(d => d.id == id);

        if (dialog != null && !string.IsNullOrEmpty(dialog.text))
            return dialog.text;
        Debug.LogWarning($"Dialog with ID {id} not found or empty.");
        return string.Empty;
    }


    // var variables = new Dictionary<string, string>()
    // {
    //     {"userName", "Thomas"},
    //     {"level", "Level 20"},
    //     {"modeName", "Survivor"}
    // };

    // string message = TextImporter.GetDialog(0, variables);
    public static string GetDialog(int id, Dictionary<string, string> variables = null)
    {
        Dialogs dialog = GameDataController.instance.data.dialogs.FirstOrDefault(d => d.id == id);

        if (dialog != null && !string.IsNullOrEmpty(dialog.text))
        {
            string result = dialog.text;
            if (variables != null)
            {
                foreach (var pair in variables)
                {
                    result = result.Replace($"{{{pair.Key}}}", pair.Value);
                }
            }

            return result;
        }

        Debug.LogWarning($"Dialog with ID {id} not found or empty.");
        return string.Empty;
    }
}

namespace Json
{


    [System.Serializable]
    public class GameData
    {
        public List<Character> characters = new List<Character>();
        public List<Monster> monsters = new List<Monster>();
        public List<Spirit> spirits = new List<Spirit>();

        public List<Reincarnation> reincarnations = new List<Reincarnation>();
        public List<ShopObject> objects = new List<ShopObject>();
        public List<Spell> spells = new List<Spell>();
        public List<Dialogs> dialogs = new List<Dialogs>();
        public List<ModeS> modes = new List<ModeS>();
        public List<FactionS> factions = new List<FactionS>();

        public List<BonusS> bonuses = new List<BonusS>(); // Ajout de la liste des bonus

    }

    [System.Serializable]
    public class Character
    {
        public int id;
        public string name;
        public string description;

        public string descriptionDechys;
        public string descriptionCircle;

        public string icon;
        public string fullIcon;
        public string pathIcon;

        public int difficulty;
        public string characterType;
        public List<int> spellIds = new List<int>();

        public List<Spell> GetSpells()
        {
            List<Spell> characterSpells = new List<Spell>();
            foreach (int id in spellIds)
            {
                Spell spell = GameDataController.instance.data.spells.Find(s => s.id == id);
                if (spell != null)
                {
                    characterSpells.Add(spell);
                }
            }
            return characterSpells;
        }
    }

    [Serializable]
    public class Monster
    {
        public string name;
        public string description;
        public string icon;
    }

    [Serializable]
    public class Spirit
    {
        public int id;
        public string name;
        public string identifier;
    }


    [Serializable]
    public class ModeS
    {
        public int id;
        public string title;
        public string description;
        public string icon;
        public string color;
    }

    [Serializable]
    public class FactionS
    {
        public int id;
        public string title;
        public string description;
        public string icon;
        public string color;
    }

    [Serializable]
    public class Reincarnation
    {
        public int id;
        public string name;
        public string description;
        public string icon;

        public int difficulty;
        public string characterType;

        public string fullIcon;
        public List<int> spellIds = new List<int>();

        public List<Spell> GetSpells()
        {
            List<Spell> characterSpells = new List<Spell>();
            foreach (int id in spellIds)
            {
                Spell spell = GameDataController.instance.data.spells.Find(s => s.id == id);
                if (spell != null)
                {
                    characterSpells.Add(spell);
                }
            }
            return characterSpells;
        }
    }

    [Serializable]
    public class ShopObject
    {
        public int id;
        public string name;
        public string description;
        public string icon;
    }

    [Serializable]
    public class Spell
    {
        public int id;
        public string name;
        public string description;
        public string icon;
    }

    [Serializable]
    public class Dialogs
    {
        public int id;
        public string text;
    }

    [Serializable]
    public class BonusS
    {
        public int id;
        public string name;
        public string description;
        public string icon;
        public string color;
        public string type; // Peut être "Attack", "Defense", "Utility"
    }
}
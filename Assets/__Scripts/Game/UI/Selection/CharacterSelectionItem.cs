
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSelectionItem : MonoBehaviour
{
    [SerializeField] private Image avatar;

    public Json.Character character;
    public Json.Reincarnation spirit;

    public int SpiritId => spirit != null ? spirit.id : -1;

    public ButtonTransition buttonTransition;

    Button button;

    public void Awake()
    {
        button = GetComponent<Button>();
        buttonTransition = GetComponent<ButtonTransition>();
    }


    public void UpdateChar(Json.Character character)
    {
        this.character = character;
        Sprite sprite = Resources.Load<Sprite>(character.icon);
        avatar.sprite = sprite;
        gameObject.name = character.name;
        button.onClick.AddListener(() => ClicChar());
    }


    public void UpdateSpirit(Json.Reincarnation spirit)
    {
        Debug.Log(spirit);
        this.spirit = spirit;
        Sprite sprite = Resources.Load<Sprite>(spirit.icon);
        avatar.sprite = sprite;
        gameObject.name = spirit.name;
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        button.onClick.AddListener(() => ClicSpirit());
    }

    public void SetAvailability(bool available)
    {
        if (!available)
        {
            buttonTransition.CloseInteraction();
            buttonTransition.GetComponent<EventTrigger>().enabled = false;
        }
        else
        {
            buttonTransition.OpenInteraction();
            buttonTransition.GetComponent<EventTrigger>().enabled = true;
        }
    }

    public void OnHoverChar()
    {
        transform.SetAsLastSibling();
    }

    public void ClicChar()
    {
        cooldownUI.instance.selectionInGame.SelectClass(character.id);
    }
    public void ClicSpirit()
    {
        cooldownUI.instance.selectionInGame.SelectSpirit(spirit.id);
    }

}
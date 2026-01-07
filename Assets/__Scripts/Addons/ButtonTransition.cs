using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

public class ButtonTransition : MonoBehaviour
{


    [Serializable]
    public enum ChangeEnum
    {
        NORMAL = 0,
        SELECTED = 1,
        HOVER = 2,
        DISABLED = 3
    }

    [SerializeField] public UnityEvent eventOnClicked;
    [SerializeField] public UnityEvent eventOnUnClicked;

    [SerializeField] public UnityEvent eventOnSelect;
    [SerializeField] public UnityEvent eventOnUnSelect;

    [SerializeField] private ObjectState[] objects;

    [SerializeField] private ImageColorState[] images;

    [SerializeField] private TextState[] texts;

    [SerializeField] private string onClickAudioClipKey;

    [SerializeField] private string openInteractionAudioClipKey;
    [SerializeField] private string closeInteractionAudioClipKey;

    [SerializeField] private string hoverInteractionAudioClipKey;
    [SerializeField] private string unHoverInteractionAudioClipKey;

    [SerializeField, HideInInspector]
    private PlayableDirector director;

    [SerializeField]
    private PlayableAsset closeInteractionAnimation;
    [SerializeField]
    private PlayableAsset openInteractionAnimation;

    [SerializeField]
    private PlayableAsset hoverInteractionAnimation;
    [SerializeField]
    private PlayableAsset unHoverInteractionAnimation;


    [HideInInspector]
    public ChangeEnum oldState = ChangeEnum.DISABLED;


    [SerializeField, HideInInspector]
    private Button button;


    [SerializeField] private bool defaultButton;

    [SerializeField] private bool letHoverButton;

    public Button Button { get => button; set => button = value; }

    private bool isClicked = false;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        button = GetComponent<Button>();
        if (onClickAudioClipKey != null) button.onClick.AddListener(PlaySoundOnClick);
        button.onClick.AddListener(PlayObjectStateClick);
    }

    private void PlayObjectStateClick()
    {
        if (button.enabled)
        {
            isClicked = !isClicked;
            if (isClicked)
            {
                eventOnClicked.Invoke();
            }
            else
            {
                eventOnUnClicked.Invoke();
            }
        }
    }

    private void PlaySoundOnClick()
    {
        SoundManager.Instance.Play2D(onClickAudioClipKey);
    }

    private void Start()
    {
        if (button.interactable)
            if (defaultButton)
            {
                ApplyChangeToElements(ChangeEnum.SELECTED);
            }
            else
            {
                PlayInstance();
            }
        else
            CloseInstance();
    }

    public void ApplyTextNormalColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (TextState text in texts)
        {
            text.ApplyNormalColor(couleur);
        }
    }

    public void ApplyImageNormalColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (ImageColorState image in images)
        {
            image.ApplyNormalColor(couleur);
        }
    }


    public void ApplyTextHoverColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (TextState text in texts)
        {
            text.ApplyHoverColor(couleur);
        }
    }

    public void ApplyImageHoverColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (ImageColorState image in images)
        {
            image.ApplyHoverColor(couleur);
        }
    }

    public void ApplyTextSelectedColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (TextState text in texts)
        {
            text.ApplySelectedColor(couleur);
        }
    }

    public void ApplyImageSelectedColor(Color color)
    {
        Color32 couleur = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        foreach (ImageColorState image in images)
        {
            image.ApplySelectedColor(couleur);
        }
    }


    public void OpenInteraction()
    {
        if (button == null) button = GetComponent<Button>();
        Button.interactable = true;
        if (GetComponent<PlayableDirector>() != null)
        {
            GetComponent<PlayableDirector>().Stop();
            director.Play(openInteractionAnimation);
        }
        if (openInteractionAudioClipKey != null)
            SoundManager.Instance.Play2D(openInteractionAudioClipKey);

        PlayInstance();
    }
    public void CloseInteraction()
    {
        if (button == null) button = GetComponent<Button>();
        Button.interactable = false;
        if (GetComponent<PlayableDirector>() != null)
        {
            GetComponent<PlayableDirector>().Stop();
            director.Play(closeInteractionAnimation);
        }
        if (closeInteractionAudioClipKey != null) SoundManager.Instance.Play2D(closeInteractionAudioClipKey);
        CloseInstance();
    }

    public void HoverInteraction()
    {
        if (oldState == ChangeEnum.SELECTED && !letHoverButton)
            return;

        if (Button.interactable)
            if (hoverInteractionAnimation != null)
                director.Play(hoverInteractionAnimation);
        foreach (ObjectState item in objects)
        {
            item.ChangeState(ChangeEnum.HOVER);
        }
        foreach (ImageColorState image in images)
        {
            image.ChangeState(ChangeEnum.HOVER);
        }
        foreach (TextState text in texts)
        {
            text.ChangeState(ChangeEnum.HOVER);
        }
        SoundManager.Instance.Play2D(hoverInteractionAudioClipKey);
    }

    public void unHoverInteraction()
    {

        if (oldState == ChangeEnum.SELECTED && !letHoverButton)
            return;

        if (Button.interactable)
            if (unHoverInteractionAnimation != null) director.Play(unHoverInteractionAnimation);
        foreach (ObjectState item in objects)
        {
            item.ChangeState(Button.interactable ? (oldState == ChangeEnum.SELECTED ? ChangeEnum.SELECTED : ChangeEnum.NORMAL) : ChangeEnum.DISABLED);
        }
        foreach (ImageColorState image in images)
        {
            image.ChangeState(Button.interactable ? (oldState == ChangeEnum.SELECTED ? ChangeEnum.SELECTED : ChangeEnum.NORMAL) : ChangeEnum.DISABLED);
        }
        foreach (TextState text in texts)
        {
            text.ChangeState(Button.interactable ? (oldState == ChangeEnum.SELECTED ? ChangeEnum.SELECTED : ChangeEnum.NORMAL) : ChangeEnum.DISABLED);
        }
        SoundManager.Instance.Play2D(unHoverInteractionAudioClipKey);
    }

    public void PlayInstance()
    {

        foreach (ObjectState item in objects)
        {
            item.ChangeState(ChangeEnum.NORMAL);
        }
        foreach (ImageColorState image in images)
        {
            image.ChangeState(ChangeEnum.NORMAL);
        }
        foreach (TextState text in texts)
        {
            text.ChangeState(ChangeEnum.NORMAL);
        }
        oldState = ChangeEnum.NORMAL;
    }
    public void CloseInstance()
    {
        foreach (ObjectState item in objects)
        {
            item.ChangeState(ChangeEnum.DISABLED);
        }
        foreach (ImageColorState image in images)
        {
            image.ChangeState(ChangeEnum.DISABLED);
        }
        foreach (TextState text in texts)
        {
            text.ChangeState(ChangeEnum.DISABLED);
        }
        oldState = ChangeEnum.DISABLED;
    }

    public void ApplyChangeToElements(ChangeEnum state)
    {
        if (state == ChangeEnum.SELECTED)
        {
            SoundManager.Instance.Play2D(openInteractionAudioClipKey);
            eventOnSelect.Invoke();
        }
        if (oldState == ChangeEnum.SELECTED)
        {
            eventOnUnSelect.Invoke();
        }

        foreach (ObjectState item in objects)
        {
            item.ChangeState(state);
        }

        foreach (TextState text in texts)
        {
            text.ChangeState(state);
        }

        foreach (ImageColorState image in images)
        {
            image.ChangeState(state);
        }
        oldState = state;

    }

    public void ApplyChangeToElements(int stateInteger)
    {
        ChangeEnum state = (ChangeEnum)stateInteger;
        if (state == ChangeEnum.SELECTED)
        {
            SoundManager.Instance.Play2D(openInteractionAudioClipKey);
        }

        foreach (ObjectState item in objects)
        {
            item.ChangeState(state);
        }

        foreach (TextState text in texts)
        {
            text.ChangeState(state);
        }

        foreach (ImageColorState image in images)
        {
            image.ChangeState(state);
        }
        oldState = state;

    }

    [System.Serializable]
    public class ObjectState
    {

        [SerializeField, HideInInspector]
        private ChangeEnum oldState = ChangeEnum.DISABLED;

        [SerializeField]
        private Image squareImg;

        [SerializeField]
        private Sprite normalSprite;

        [SerializeField]
        private Sprite selectedSprite;

        [SerializeField]
        private Sprite hoverSprite;

        [SerializeField]
        private Sprite desactivatedSprite;


        public void ChangeState(ChangeEnum state)
        {
            switch (state)
            {
                case ChangeEnum.NORMAL:
                    squareImg.sprite = normalSprite;
                    break;
                case ChangeEnum.SELECTED:
                    squareImg.sprite = selectedSprite;
                    break;
                case ChangeEnum.HOVER:
                    squareImg.sprite = hoverSprite;
                    break;
                case ChangeEnum.DISABLED:
                    squareImg.sprite = desactivatedSprite;
                    break;
            }
        }


    }

    [System.Serializable]
    public class TextState
    {
        [SerializeField, HideInInspector]
        private ChangeEnum oldState = ChangeEnum.DISABLED;

        [SerializeField]
        private TextMeshProUGUI text;

        [SerializeField]
        private Color32 normalText;

        [SerializeField]
        private Color32 selectedText;

        [SerializeField]
        private Color32 hoverText;

        [SerializeField]
        private Color32 desactivatedText;
        [SerializeField]
        private bool ignoreChangeColor;

        public void ApplyNormalColor(Color32 color)
        {

            if (ignoreChangeColor)
                return;
            normalText = color;
        }

        public void ApplySelectedColor(Color32 color)
        {
            if (ignoreChangeColor)
                return;
            selectedText = color;
        }

        public void ApplyHoverColor(Color32 color)
        {
            if (ignoreChangeColor)
                return;
            hoverText = color;
        }

        public void ChangeState(ChangeEnum state)
        {
            switch (state)
            {
                case ChangeEnum.NORMAL:
                    text.color = normalText;
                    break;
                case ChangeEnum.SELECTED:
                    text.color = selectedText;
                    break;
                case ChangeEnum.HOVER:
                    text.color = hoverText;
                    break;
                case ChangeEnum.DISABLED:
                    text.color = desactivatedText;
                    break;

                default:
                    if (oldState == ChangeEnum.DISABLED)
                    {
                        ChangeState(ChangeEnum.NORMAL);
                    }
                    else
                    {
                        ChangeState(oldState);
                    }
                    break;
            }
            oldState = state;
        }


    }


    [System.Serializable]
    public class ImageColorState
    {
        [SerializeField, HideInInspector]
        private ChangeEnum oldState = ChangeEnum.DISABLED;

        [SerializeField]
        private Image image;

        [SerializeField]
        private Color32 normalText;

        [SerializeField]
        private Color32 selectedText;

        [SerializeField]
        private Color32 hoverText;

        [SerializeField]
        private Color32 desactivatedText;

        [SerializeField]
        private bool ignoreChangeColor;


        public void ApplyNormalColor(Color32 color)
        {
            if (ignoreChangeColor)
                return;
            normalText = color;
        }

        public void ApplySelectedColor(Color32 color)
        {
            if (ignoreChangeColor)
                return;
            selectedText = color;
        }

        public void ApplyHoverColor(Color32 color)
        {
            if (ignoreChangeColor)
                return;
            hoverText = color;
        }

        public void ChangeState(ChangeEnum state)
        {
            switch (state)
            {
                case ChangeEnum.NORMAL:
                    image.color = normalText;
                    break;
                case ChangeEnum.SELECTED:
                    image.color = selectedText;
                    break;
                case ChangeEnum.HOVER:
                    image.color = hoverText;
                    break;
                case ChangeEnum.DISABLED:
                    image.color = desactivatedText;
                    break;

                default:
                    if (oldState == ChangeEnum.DISABLED)
                    {
                        ChangeState(ChangeEnum.NORMAL);
                    }
                    else
                    {
                        ChangeState(oldState);
                    }
                    break;
            }
            oldState = state;
        }


    }
}
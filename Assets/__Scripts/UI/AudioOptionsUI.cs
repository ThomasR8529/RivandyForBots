using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsUI : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Slider voiceSlider;
    [SerializeField] private Slider ambientSlider;

    private bool _initialized;

    private void Awake()
    {
        // optional: set slider ranges if not set in inspector
        InitSlider(masterSlider);
        InitSlider(musicSlider);
        InitSlider(sfxSlider);
        InitSlider(uiSlider);
        InitSlider(voiceSlider);
        InitSlider(ambientSlider);
    }

    private void OnEnable()
    {
        RefreshFromPrefs();
        HookEvents(true);
        _initialized = true;
    }

    private void OnDisable()
    {
        HookEvents(false);
        _initialized = false;
    }

    private void InitSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
    }

    private void HookEvents(bool hook)
    {
        if (hook)
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            if (uiSlider != null) uiSlider.onValueChanged.AddListener(OnUIChanged);
            if (voiceSlider != null) voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
            if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(OnAmbientChanged);
        }
        else
        {
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            if (uiSlider != null) uiSlider.onValueChanged.RemoveListener(OnUIChanged);
            if (voiceSlider != null) voiceSlider.onValueChanged.RemoveListener(OnVoiceChanged);
            if (ambientSlider != null) ambientSlider.onValueChanged.RemoveListener(OnAmbientChanged);
        }
    }

    public void RefreshFromPrefs()
    {
        if (SoundManager.Instance == null) return;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(SoundManager.Instance.GetMasterVolume());
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(SoundManager.Instance.GetMusicVolume());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetSFXVolume());
        if (uiSlider != null) uiSlider.SetValueWithoutNotify(SoundManager.Instance.GetUIVolume());
        if (voiceSlider != null) voiceSlider.SetValueWithoutNotify(SoundManager.Instance.GetVoiceVolume());
        if (ambientSlider != null) ambientSlider.SetValueWithoutNotify(SoundManager.Instance.GetAmbientVolume());
    }

    private void OnMasterChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetMasterVolume(v);
        Debug.Log("Master changed to " + v);
    }
    private void OnMusicChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetMusicVolume(v);
    }
    private void OnSFXChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetSFXVolume(v);
    }
    private void OnUIChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetUIVolume(v);
    }
    private void OnVoiceChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetVoiceVolume(v);
    }
    private void OnAmbientChanged(float v)
    {
        if (!_initialized || SoundManager.Instance == null) return;
        SoundManager.Instance.SetAmbientVolume(v);
    }
}


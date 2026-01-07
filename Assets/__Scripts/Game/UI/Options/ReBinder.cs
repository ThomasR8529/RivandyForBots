using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Option
{
    public string name;
    public GameObject value;
}

public class ReBinder : MonoBehaviour
{
    public static ReBinder Instance { get; private set; }

    [SerializeField] private List<ReBindUI> liste;
    [SerializeField] private List<Option> listeOptions;

    private bool isInitializing = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadPrefs();
        isInitializing = false;
        foreach (ReBindUI item in liste)
        {
            if (item != null)
                InputManager.LoadBindingOverride(item.actionName);
        }
    }

    public void LoadPrefs()
    {
        foreach (Option item in listeOptions)
        {
            if (item.value != null && item.value.TryGetComponent(out Toggle toggle))
            {
                toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(item.name, 0) == 1);
            }
            else if (item.value != null && item.value.TryGetComponent(out TMP_InputField input))
            {
                input.SetTextWithoutNotify(PlayerPrefs.GetString(item.name, ""));
            }
            else if (item.value != null && item.value.TryGetComponent(out TMP_Dropdown dropdown))
            {
                dropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(item.name, 0));
            }
            else if (item.value != null && item.value.TryGetComponent(out Slider slider))
            {
                slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(item.name, 0f));
            }
        }
    }

    // Appelée explicitement via un bouton "Appliquer" ou "Sauvegarder"
    public void ApplyChanges()
    {
        foreach (Option item in listeOptions)
        {
            if (item.value.TryGetComponent(out Slider slider))
            {
                PlayerPrefs.SetFloat(item.name, slider.value);
                NotifyChange(item.name, slider.value);
            }
            else if (item.value.TryGetComponent(out TMP_InputField inputField))
            {
                PlayerPrefs.SetString(item.name, inputField.text);
                NotifyChange(item.name, inputField.text);
            }
            else if (item.value.TryGetComponent(out TMP_Dropdown dropdown))
            {
                PlayerPrefs.SetInt(item.name, dropdown.value);
                NotifyChange(item.name, dropdown.value);
            }
            else if (item.value.TryGetComponent(out Toggle toggle))
            {
                PlayerPrefs.SetInt(item.name, toggle.isOn ? 1 : 0);
                NotifyChange(item.name, toggle.isOn ? 1 : 0);
            }
        }

        PlayerPrefs.Save();
    }

    private void NotifyChange<T>(string key, T value)
    {
        if (isInitializing)
            return;  // Bloque les notifications pendant l'initialisation

        switch (key)
        {
            case "fastCast":
                int newValue = Convert.ToInt32(value);
                if (GameController.instance?.playerReference != null && GameController.instance?.playerReference?.playerShooting?.isFastCast != (newValue == 1))
                {
                    GameController.instance.playerReference.playerShooting.SetFastCast(newValue);
                    PlayerPrefs.SetInt("fastCast", newValue);
                }
                if (GameController.instance?.playerReference == null)
                {
                    PlayerPrefs.SetInt("fastCast", newValue);
                }
                break;

            case "screenMode":
                ApplyScreenMode(Convert.ToInt32(value));
                PlayerPrefs.SetString("screenMode", value.ToString());
                break;
            case "language":
                GameDataController.instance.SetLanguage(value.ToString());
                PlayerPrefs.SetString("language", value.ToString());
                break;

                // Ajoute ici d'autres options si nécessaire
        }

        PlayerPrefs.Save();
    }

    public static void ApplyScreenMode(int mode)
    {
        int screenWidth = Display.main.systemWidth;
        int screenHeight = Display.main.systemHeight;

        switch (mode)
        {
            // Mode Fenêtré adaptatif (90% de la taille de l'écran)
            case 0:
                int windowedWidth = Mathf.RoundToInt(screenWidth);
                int windowedHeight = Mathf.RoundToInt(screenHeight);
                Screen.fullScreenMode = FullScreenMode.Windowed;
                CenterWindow(windowedWidth, windowedHeight);
                break;

            // Plein écran exclusif (performance maximale)
            case 1:
                Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.ExclusiveFullScreen);
                break;

            // Plein écran fenêtré sans bordures (borderless window)
            case 2:
                Screen.SetResolution(screenWidth, screenHeight, FullScreenMode.FullScreenWindow);
                break;

            default:
                Debug.LogWarning($"Unknown screen mode: {mode}. Defaulting to Windowed.");
                ApplyScreenMode(0);
                break;
        }

        Debug.Log($"Screen mode set to: {mode}");
    }

    // Méthode optionnelle pour centrer la fenêtre en mode fenêtré (Windows uniquement)
    private static void CenterWindow(int width, int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    var hwnd = GetActiveWindow();
    int screenWidth = Display.main.systemWidth;
    int screenHeight = Display.main.systemHeight;
    int posX = (screenWidth - width) / 2;
    int posY = (screenHeight - height) / 2;
    SetWindowPos(hwnd, IntPtr.Zero, posX, posY, width, height, 0);
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
[System.Runtime.InteropServices.DllImport("user32.dll")]
private static extern IntPtr GetActiveWindow();

[System.Runtime.InteropServices.DllImport("user32.dll")]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
#endif

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}

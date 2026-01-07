using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ReBindUI : MonoBehaviour
{
    [SerializeField]
    private InputActionReference inputActionReference;

    [SerializeField]
    private bool excludeMouse = true;
    [Range(0, 10)]
    [SerializeField]
    private int selectedBinding;
    [SerializeField]
    private InputBinding.DisplayStringOptions displayStringOptions;

    [Header("Binding Info - DO NOT EDIT")]
    [SerializeField]
    private InputBinding inputBinding;
    private int bindingIndex;

    public string actionName;
    private string compositePart;

    [Header("UI Fields (Optional)")]
    [SerializeField]
    private TextMeshProUGUI actionText;
    [SerializeField]
    private Button rebindButton;
    [SerializeField]
    private TextMeshProUGUI rebindText;
    [SerializeField]
    private Button resetButton;

    private void Start()
    {
        if (inputActionReference != null)
        {
            actionName = inputActionReference.action.name;
            InputManager.LoadBindingOverride(actionName);
            inputActionReference.action.Disable();
            inputActionReference.action.Enable();
        }
    }

    private void OnEnable()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(DoRebind);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetBinding);

        if (inputActionReference != null)
        {
            actionName = inputActionReference.action.name;
            InputManager.LoadBindingOverride(actionName);
            GetBindingInfo();
            UpdateUI();
        }

        InputManager.rebindComplete += OnRebindComplete;
        InputManager.rebindCanceled += UpdateUI;
        inputActionReference.action.Disable();
    }

    private void OnDisable()
    {
        if (rebindButton != null)
            rebindButton.onClick.RemoveListener(DoRebind);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetBinding);

        InputManager.rebindComplete -= OnRebindComplete;
        InputManager.rebindCanceled -= UpdateUI;
        inputActionReference.action.Enable();
    }

    private void OnValidate()
    {
        if (inputActionReference == null)
            return;

        GetBindingInfo();
        UpdateUI();
    }

    private void GetBindingInfo()
    {
        if (inputActionReference.action != null)
        {
            actionName = inputActionReference.action.name;
        }

        if (inputActionReference.action.bindings.Count > selectedBinding)
        {
            inputBinding = inputActionReference.action.bindings[selectedBinding];
            bindingIndex = selectedBinding;
            compositePart = inputBinding.name;
        }
    }

    private void UpdateUI()
    {

        if (rebindText != null)
        {
            FillKeyboardApi.Instance?.UpdateKeys();
            if (Application.isPlaying)
                rebindText.text = InputManager.GetBindingName(actionName, bindingIndex);
            else
                rebindText.text = inputActionReference.action.GetBindingDisplayString(bindingIndex);
        }
    }

    private void DoRebind()
    {
        InputManager.StartRebind(actionName, bindingIndex, rebindText, excludeMouse);
    }

    private void ResetBinding()
    {
        InputManager.ResetBinding(actionName, bindingIndex);
        UpdateUI();
    }

    private string GetBindingPrefsKey(string actionName, int bindingIndex)
    {
        return actionName + "_" + bindingIndex;
    }

    private void SaveBindingToPrefs()
    {
        if (inputBinding != null && inputActionReference != null)
        {
            string key = GetBindingPrefsKey(actionName, bindingIndex);
            string json = JsonUtility.ToJson(inputBinding);
            PlayerPrefs.SetString(key, json);
        }
    }

    private void OnRebindComplete()
    {
        SaveBindingToPrefs();
        UpdateUI();
    }

    public void TakeFromPrefs()
    {
        if (inputActionReference != null)
        {
            if (string.IsNullOrEmpty(actionName))
                actionName = inputActionReference.action.name;

            InputManager.LoadBindingOverride(actionName);
            GetBindingInfo();
            UpdateUI();
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public static PlayerInputActions inputActions;

    public static event Action rebindComplete;
    public static event Action rebindCanceled;
    public static event Action<InputAction, int> rebindStarted;

    public UnityEvent onScreenModeEnabled;
    public UnityEvent onScreenModeClosed;

    public GameObject echapMenu;
    public InputSystemCustom inputSystemCustom;

    bool echap = false;
    bool screenMode = false;

    public static InputManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (inputActions == null)
            inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Echap.performed += Echap;
        inputActions.Player.ScreenMode.performed += ScreenModeFunc;
        inputActions.Player.Bonus1.performed += Bonus1;
        inputActions.Player.Bonus2.performed += Bonus2;
        inputActions.Player.Bonus3.performed += Bonus3;

        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Echap.performed -= Echap;
            inputActions.Player.ScreenMode.performed -= ScreenModeFunc;
            inputActions.Player.Bonus1.performed -= Bonus1;
            inputActions.Player.Bonus2.performed -= Bonus2;
            inputActions.Player.Bonus3.performed -= Bonus3;
            inputActions.Player.Disable();
        }
    }

    private void OnDestroy()
    {
        OnDisable();
    }

    private void Echap(InputAction.CallbackContext obj)
    {
        echap = !echap;
        echapMenu.SetActive(echap);
        Debug.Log("ECHAP PRESSED");
        Cursor.lockState = echap ? CursorLockMode.Confined : CursorLockMode.Locked;
    }
    private void ScreenModeFunc(InputAction.CallbackContext obj)
    {
        screenMode = !screenMode;
        if (screenMode)
        {
            onScreenModeEnabled.Invoke();
            GameController.instance.playerReference.playerClasses.SetCharacterMeshVisible(false);
        }
        else
        {
            onScreenModeClosed.Invoke();
            GameController.instance.playerReference.playerClasses.SetCharacterMeshVisible(true);
        }
    }
    private void Bonus1(InputAction.CallbackContext obj)
    {
        // BonusSelectionUI.instance.SelectBonus(BonusSelectionUI.instance.bonusItems[0].bonus.id);
    }

    private void Bonus2(InputAction.CallbackContext obj)
    {
        // BonusSelectionUI.instance.SelectBonus(BonusSelectionUI.instance.bonusItems[1].bonus.id);
    }

    private void Bonus3(InputAction.CallbackContext obj)
    {
        // BonusSelectionUI.instance.SelectBonus(BonusSelectionUI.instance.bonusItems[2].bonus.id);
    }
    public static void StartRebind(string actionName, int bindingIndex, TextMeshProUGUI statusText, bool excludeMouse)
    {
        Debug.Log(actionName);
        InputAction action = inputActions.asset.FindAction(actionName);
        if (action == null || action.bindings.Count <= bindingIndex)
        {
            Debug.Log("Couldn't find action or binding");
            return;
        }

        if (action.bindings[bindingIndex].isComposite)
        {
            var firstPartIndex = bindingIndex + 1;
            if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isComposite)
                if (!SceneManager.GetActiveScene().name.Equals("Home")) instance.DoRebind(action, bindingIndex, statusText, true, excludeMouse);
                else
                {
                    DoStaticRebind(action, bindingIndex, statusText, true, excludeMouse);
                }
        }
        else
        {
            if (!SceneManager.GetActiveScene().name.Equals("Home")) instance.DoRebind(action, bindingIndex, statusText, false, excludeMouse);
            else
            {
                DoStaticRebind(action, bindingIndex, statusText, false, excludeMouse);
            }

        }
    }

    private void DoRebind(InputAction actionToRebind, int bindingIndex, TextMeshProUGUI statusText, bool allCompositeParts, bool excludeMouse)
    {
        if (actionToRebind == null || bindingIndex < 0)
            return;

        statusText.text = $"Press a {actionToRebind.expectedControlType}";

        actionToRebind.Disable();

        var rebind = actionToRebind.PerformInteractiveRebinding(bindingIndex);

        rebind.OnComplete(operation =>
        {
            actionToRebind.Enable();
            operation.Dispose();

            if (allCompositeParts)
            {
                var nextBindingIndex = bindingIndex + 1;
                if (nextBindingIndex < actionToRebind.bindings.Count && actionToRebind.bindings[nextBindingIndex].isPartOfComposite)
                    DoRebind(actionToRebind, nextBindingIndex, statusText, allCompositeParts, excludeMouse);
            }

            SaveBindingOverride(actionToRebind);
            rebindComplete?.Invoke();

            // Reload input actions in InputSystemCustom
            if (inputSystemCustom != null)
            {
                inputSystemCustom.ReloadInputActions();
            }
            FillKeyboardApi.Instance?.UpdateKeys();
        });


        rebind.OnCancel(operation =>
        {
            actionToRebind.Enable();
            operation.Dispose();

            rebindCanceled?.Invoke();
        });

        rebind.WithCancelingThrough("<Keyboard>/escape");

        if (excludeMouse)
            rebind.WithControlsExcluding("Mouse");

        rebindStarted?.Invoke(actionToRebind, bindingIndex);
        rebind.Start(); //actually starts the rebinding process
    }


    public static void DoStaticRebind(InputAction actionToRebind, int bindingIndex, TextMeshProUGUI statusText, bool allCompositeParts, bool excludeMouse)
    {
        if (actionToRebind == null || bindingIndex < 0)
            return;

        statusText.text = $"Press a {actionToRebind.expectedControlType}";

        actionToRebind.Disable();

        var rebind = actionToRebind.PerformInteractiveRebinding(bindingIndex);

        rebind.OnComplete(operation =>
        {
            actionToRebind.Enable();
            operation.Dispose();

            if (allCompositeParts)
            {
                var nextBindingIndex = bindingIndex + 1;
                if (nextBindingIndex < actionToRebind.bindings.Count && actionToRebind.bindings[nextBindingIndex].isPartOfComposite)
                    DoStaticRebind(actionToRebind, nextBindingIndex, statusText, allCompositeParts, excludeMouse);
            }

            SaveBindingOverride(actionToRebind);
            rebindComplete?.Invoke();

            FillKeyboardApi.Instance?.UpdateKeys();
        });


        rebind.OnCancel(operation =>
        {
            actionToRebind.Enable();
            operation.Dispose();

            rebindCanceled?.Invoke();
        });

        rebind.WithCancelingThrough("<Keyboard>/escape");

        if (excludeMouse)
            rebind.WithControlsExcluding("Mouse");

        rebindStarted?.Invoke(actionToRebind, bindingIndex);
        rebind.Start(); //actually starts the rebinding process
    }



    public static string GetBindingName(string actionName, int bindingIndex)
    {
        if (inputActions == null)
            inputActions = new PlayerInputActions();

        InputAction action = inputActions.asset.FindAction(actionName);
        return action.GetBindingDisplayString(bindingIndex);
    }

    private static void SaveBindingOverride(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            PlayerPrefs.SetString(action.actionMap + action.name + i, action.bindings[i].overridePath);
        }
    }

    public static void LoadBindingOverride(string actionName)
    {

        if (inputActions == null)
            inputActions = new PlayerInputActions();

        InputAction action = inputActions.asset.FindAction(actionName);
        if (action == null) return;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            string overridePath = PlayerPrefs.GetString(action.actionMap + action.name + i, null);
            if (!string.IsNullOrEmpty(overridePath))
                action.ApplyBindingOverride(i, overridePath);
        }
    }

    public static void ResetBinding(string actionName, int bindingIndex)
    {
        InputAction action = inputActions.asset.FindAction(actionName);

        if (action == null || action.bindings.Count <= bindingIndex)
        {
            Debug.Log("Could not find action or binding");
            return;
        }

        if (action.bindings[bindingIndex].isComposite)
        {
            for (int i = bindingIndex; i < action.bindings.Count && action.bindings[i].isComposite; i++)
                action.RemoveBindingOverride(i);
        }
        else
            action.RemoveBindingOverride(bindingIndex);

        SaveBindingOverride(action);
    }

}


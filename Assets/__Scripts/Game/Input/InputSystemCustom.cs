
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Lightbug.CharacterControllerPro.Implementation;
using System;
using Unity.VisualScripting;

public class InputSystemCustom : InputHandler
{
    [SerializeField]
    bool filterByActionMap = false;

    [SerializeField]
    string gameplayActionMap = "Player";

    [SerializeField]
    bool filterByControlScheme = false;

    [SerializeField]
    string controlSchemeName = "Keyboard";

    Dictionary<string, InputAction> inputActionsDictionary = new Dictionary<string, InputAction>();

    [SerializeField] CharacterBrain characterBrain;

    [SerializeField] PlayerReference playerReference;

    protected virtual void Awake()
    {
        characterBrain = GetComponent<CharacterBrain>();
        if (playerReference == null)
        {
            playerReference = GetComponent<PlayerReference>();
        }

        // Ensure a valid input actions asset exists early to avoid race with InputManager
        if (InputManager.inputActions == null)
        {
            InputManager.inputActions = new PlayerInputActions();
        }

        // Register to InputManager instance if available (optional)
        if (InputManager.instance != null && InputManager.instance.inputSystemCustom == null)
        {
            InputManager.instance.inputSystemCustom = this;
        }

        InitializeInputActions();
    }

    private void OnEnable()
    {
        // If for any reason actions were not initialized at Awake, try again on enable
        if (inputActionsDictionary.Count == 0)
        {
            InitializeInputActions();
        }
    }

    private void InitializeInputActions()
    {
        // Clean any previous subscriptions to prevent duplicates
        OnDisable();

        if (InputManager.inputActions == null)
        {
            InputManager.inputActions = new PlayerInputActions();
        }

        if (InputManager.inputActions.asset == null)
        {
            Debug.Log("No input actions asset found! Creating a new one.");
            InputManager.inputActions = new PlayerInputActions();
        }

        InputManager.inputActions.asset.Enable();

        if (filterByControlScheme)
        {
            string bindingGroup = InputManager.inputActions.asset.controlSchemes.First(x => x.name == controlSchemeName).bindingGroup;
            InputManager.inputActions.asset.bindingMask = InputBinding.MaskByGroup(bindingGroup);
        }

        inputActionsDictionary.Clear();
        ReadOnlyArray<InputAction> rawInputActions;

        if (filterByActionMap)
        {
            rawInputActions = InputManager.inputActions.asset.FindActionMap(gameplayActionMap).actions;
        }
        else
        {
            List<InputAction> allActions = new List<InputAction>();
            foreach (var actionMap in InputManager.inputActions.asset.actionMaps)
            {
                allActions.AddRange(actionMap.actions);
            }
            rawInputActions = new ReadOnlyArray<InputAction>(allActions.ToArray());
        }

        foreach (var action in rawInputActions)
        {
            inputActionsDictionary[action.name] = action;
            action.performed += OnActionPerformed;
            action.canceled += OnActionCanceled;
        }
    }

    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        var actionName = context.action.name;
        if (actionName == "Jump")
        {
            characterBrain.characterActions.jump.value = false;
        }
    }
    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        var actionName = context.action.name;

        // Vérifie si le joueur est bloqué AVANT d'exécuter une action
        if (IsBlocked())
        {
            if (actionName == "Movement")
            {
                characterBrain.characterActions.movement.value = Vector2.zero;
            }
            // Empêche toute modification des actions pendant le blocage
            return;
        }

        if (actionName == "Movement")
        {
            Vector2 value = context.ReadValue<Vector2>();
            characterBrain.characterActions.movement.value = value;
        }
        else
        {
            bool value = context.ReadValue<float>() > 0;

            if (actionName == "Jump")
            {
                // Block jump if a timed blocker is active on the local player
                if (playerReference != null && playerReference.playerMovement != null && playerReference.playerMovement.IsJumpBlocked())
                {
                    characterBrain.characterActions.jump.value = false;
                    return;
                }
                characterBrain.characterActions.jump.value = value;
            }
            if (actionName == "Dash")
            {
                bool isDashing = playerReference.playerDash.DashAction();
                if (isDashing)
                {
                    characterBrain.characterActions.dash.value = value;
                }
            }
            else if (actionName == "run")
            {
                characterBrain.characterActions.run.value = value;
            }
            else if (actionName == "crouch")
            {
                characterBrain.characterActions.crouch.value = value;
            }
            else if (actionName == "jetPack")
            {
                characterBrain.characterActions.jetPack.value = value;
            }
            else if (actionName == "interact")
            {
                characterBrain.characterActions.interact.value = value;
            }
        }
    }

    public override bool GetBool(string actionName)
    {
        InputAction inputAction;
        if (!IsBlocked() && inputActionsDictionary.TryGetValue(actionName, out inputAction))
        {
            return inputAction.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint;
        }
        return false;
    }

    public override float GetFloat(string actionName)
    {
        InputAction inputAction;
        if (inputActionsDictionary.TryGetValue(actionName, out inputAction) && !IsBlocked())
        {
            return inputAction.ReadValue<float>();
        }
        return 0f;
    }

    public override Vector2 GetVector2(string actionName)
    {
        InputAction inputAction;
        if (inputActionsDictionary.TryGetValue(actionName, out inputAction) && !IsBlocked())
        {
            return inputAction.ReadValue<Vector2>();
        }
        return Vector2.zero;
    }

    public void ReloadInputActions()
    {
        inputActionsDictionary.Clear();
        InitializeInputActions();
        Debug.Log("Reload inputs");
    }

    private void OnDisable()
    {
        foreach (var action in inputActionsDictionary.Values)
        {
            action.performed -= OnActionPerformed;
            action.canceled -= OnActionCanceled;
        }
    }

    private void OnDestroy()
    {
        OnDisable();
    }

    public bool IsBlocked()
    {
        return isServerBlock || isParalyzed || isFrozen || isSleeping || isStunnedAir || isStunned || isCasting || isDead || blockMove;
    }

    public override void ForceUpdateMovement()
    {
        if (inputActionsDictionary.TryGetValue("Movement", out var movementAction))
        {
            Vector2 movementValue = movementAction.ReadValue<Vector2>();
            characterBrain.characterActions.movement.value = movementValue;
        }
    }

}

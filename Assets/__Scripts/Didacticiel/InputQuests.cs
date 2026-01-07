using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InputQuests : NetworkBehaviour
{


    [SerializeField] UnityEvent onObjectiveDone;
    [SerializeField] Toggle[] toggles;

#if !UNITY_SERVER

    private void Start() {
        InputManager.inputActions.Player.Spell1.performed += Spell1Action;
        InputManager.inputActions.Player.Spell2.performed += Spell2Action;
        InputManager.inputActions.Player.Spell3.performed += Spell3Action;
        InputManager.inputActions.Player.Spell4.performed += Spell4Action;
    }



    private void Spell4Action(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[4].isOn = true;
    }

    private void Spell3Action(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[3].isOn = true;
    }

    private void Spell2Action(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[2].isOn = true;
    }

    private void Spell1Action(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[1].isOn = true;

    }


    private void Update() {
        if (!gameObject.activeSelf)
            return;
        if (Input.GetMouseButton(0)) {
            toggles[0].isOn = true;
            // Debloquer l'icone autoAttack
        }

        if (toggles.All(t => t.isOn)) {
            onObjectiveDone.Invoke();
        }
    }


#endif



}

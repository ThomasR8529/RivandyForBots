using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JumpQuests : NetworkBehaviour
{


    [SerializeField] UnityEvent onObjectiveDone;
    [SerializeField] Toggle[] toggles;


    private void Start() {
        if (IsServer) {
            Destroy(gameObject);
        }
        InputManager.inputActions.Player.Dash.performed += DashAction;
        InputManager.inputActions.Player.Jump.performed += JumpAction;

    }



    private void DashAction(InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[1].isOn = true;
    }

    private void JumpAction(InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        toggles[0].isOn = true;
    }


    private void Update() {

        if(toggles.All( t => t.isOn)) {
            onObjectiveDone.Invoke();
        }
    }

}

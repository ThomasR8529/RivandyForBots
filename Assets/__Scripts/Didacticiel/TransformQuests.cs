using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TransformQuests : MonoBehaviour
{


    [SerializeField] UnityEvent onObjectiveDone;

#if !UNITY_SERVER
    private void Start() {
        InputManager.inputActions.Player.Reincarnation.performed += ReincarnationAction;

    }



    private void ReincarnationAction(InputAction.CallbackContext obj) {
        if (!gameObject.activeSelf)
            return;
        onObjectiveDone.Invoke();
    }

#endif

}

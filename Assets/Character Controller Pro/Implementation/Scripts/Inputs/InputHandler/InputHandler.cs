using UnityEngine;
using Unity.Netcode;
using System;
using Lightbug.CharacterControllerPro.Implementation;
using Lightbug.CharacterControllerPro.Demo;
/// <summary>
/// Defines the nature of the inputs obtained by the associated input handler.
/// </summary>
public enum HumanInputType
{
    InputManager,
    UIMobile,
    Custom
}

/// <summary>
/// This abstract class contains all the input methods that are used by the character brain. This is the base class for all the input detection methods available.
/// </summary>
public abstract class InputHandler : MonoBehaviour
{

    public bool isCasting;
    public bool blockMove;
    public bool isStunned;
    public bool isFrozen;
    public bool isStunnedAir;
    public bool isParalyzed;
    public bool isSleeping;

    public bool isServerBlock;
    public bool jumpValue;
    public bool isDead;
    public bool run;
    public bool interact;
    public bool jetPack;
    public bool dash;
    public bool crouch;



    public static InputHandler CreateInputHandler(GameObject gameObject, HumanInputType inputType)
    {
        InputHandler inputHandler = null;

        switch (inputType)
        {
            case HumanInputType.InputManager:
                inputHandler = gameObject.AddComponent<UnityInputHandler>();
                //gameObject.GetComponent<Camera3D>().inputHandler = (UnityInputHandler)inputHandler;
                break;
            case HumanInputType.UIMobile:

                inputHandler = gameObject.AddComponent<UIInputHandler>();

                break;
        }

        return inputHandler;
    }

    public abstract void ForceUpdateMovement();
    public abstract bool GetBool(string actionName);
    public abstract float GetFloat(string actionName);
    public abstract Vector2 GetVector2(string actionName);


    /*        [ServerRpc]
            public void SetJumpServerRpc(bool output) {
                jumpValue = output;
            }

            [ServerRpc]
            public void SetRunServerRpc(bool output) {
                run = output;
            }

            [ServerRpc]
            public void SetInteractServerRpc(bool output) {
               interact = output;
            }

            [ServerRpc]
            public void SetJetPackServerRpc(bool output) {
                jetPack = output;
            }

            [ServerRpc]
            public void SetDashServerRpc(bool output) {
                dash = output;
            }

            [ServerRpc]
            public void SetCrouchServerRpc(bool output) {
                crouch = output;
            }


            [ServerRpc]
            public void SetPitchServerRpc(float output) {
                pitch = output;
            }

            [ServerRpc]
            public void SetRollServerRpc(float output) {
                roll = output;
            }

            [ServerRpc]
            public void SetMovementServerRpc(Vector2 output) {
                movement = output;
            }

    */
    public bool IsDashing()
    {
        return dash ? true : false;
    }

}
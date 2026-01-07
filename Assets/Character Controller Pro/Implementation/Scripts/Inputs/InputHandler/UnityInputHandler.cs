using UnityEngine;
using System.Collections.Generic;
using Lightbug.CharacterControllerPro.Implementation;
using System;

/// <summary>
/// This input handler implements the input detection following the Unity's Input Manager convention. This scheme is used for desktop games.
/// </summary>
public class UnityInputHandler : InputHandler
{

    struct Vector2Action
    {
        public string x;
        public string y;

        public Vector2Action(string x, string y)
        {
            this.x = x;
            this.y = y;
        }
    }

    Dictionary<string, Vector2Action> vector2Actions = new Dictionary<string, Vector2Action>();

    public override bool GetBool(string actionName)
    {
        bool output = false;
        try
        {
            output = Input.GetButton(actionName);
        }
        catch (System.Exception)
        {
            PrintInputWarning(actionName);
        }


        switch (actionName)
        {
            case "Jump":
                if (jumpValue != output)
                {
                    jumpValue = output;
                    //SetJumpServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) jumpValue = false;
                break;
            case "Run":
                if (run != output)
                {
                    run = output;
                    //SetRunServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) run = false;

                break;
            case "Interact":
                if (interact != output)
                {
                    interact = output;
                    //SetInteractServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) interact = false;
                break;
            case "Jet Pack":
                if (jetPack != output)
                {
                    jetPack = output;
                    //SetJetPackServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) jetPack = false;
                break;
            case "Dash":
                if (dash != output)
                {
                    dash = output;
                    //SetDashServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) dash = false;
                break;
            case "Crouch":
                if (crouch != output)
                {
                    crouch = output;
                    //SetCrouchServerRpc(output);
                }
                if (isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) crouch = false;
                break;
        }
        return (isDead || isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) ? false : output;
    }

    public override float GetFloat(string actionName)
    {
        float output = default(float);
        try
        {
            output = Input.GetAxis(actionName);
        }
        catch (System.Exception)
        {
            PrintInputWarning(actionName);
        }


        return output;
    }

    public override Vector2 GetVector2(string actionName)
    {
        // Not officially supported	by Unity's input manager.
        // Example : "Movement"  splits into "Movement X" and "Movement Y"

        Vector2Action vector2Action;

        bool found = vector2Actions.TryGetValue(actionName, out vector2Action);

        if (!found)
        {
            vector2Action = new Vector2Action(
                string.Concat(actionName, " X"),
                string.Concat(actionName, " Y")
            );

            vector2Actions.Add(actionName, vector2Action);
        }

        Vector2 output = default(Vector2);

        try
        {
            output = new Vector2(Input.GetAxis(vector2Action.x), Input.GetAxis(vector2Action.y));
        }
        catch (System.Exception)
        {
            PrintInputWarning(vector2Action.x, vector2Action.y);
        }


        switch (actionName)
        {
            case "Movement":
                if (isDead || isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) Debug.Log("Etat inactif impossible");
                break;
        }


        return (isDead || isStunned || isFrozen || isParalyzed || isSleeping || isStunnedAir || isServerBlock) ? Vector2.zero : output;
    }

    void PrintInputWarning(string actionName)
    {
        Debug.LogWarning($"{actionName} action not found! Please make sure this action is included in your input settings (axis). If you're only testing the demo scenes from " +
        "Character Controller Pro please load the input preset included at \"Character Controller Pro/OPEN ME/Presets/.");
    }

    void PrintInputWarning(string actionXName, string actionYName)
    {
        Debug.LogWarning($"{actionXName} and/or {actionYName} actions not found! Please make sure both of these actions are included in your input settings (axis). If you're only testing the demo scenes from " +
        "Character Controller Pro please load the input preset included at \"Character Controller Pro/OPEN ME/Presets/.");
    }

    public override void ForceUpdateMovement()
    {
        throw new NotImplementedException();
    }
}

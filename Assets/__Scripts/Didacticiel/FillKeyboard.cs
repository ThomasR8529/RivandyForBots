using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FillKeyboard : MonoBehaviour
{

    public static FillKeyboard instance;

    [SerializeField] TextMeshProUGUI texteDash;
    [SerializeField] TextMeshProUGUI texteSpell;
    [SerializeField] TextMeshProUGUI texteSpell2;
    [SerializeField] TextMeshProUGUI texteMove;
    [SerializeField] TextMeshProUGUI texteTransform;

#if !UNITY_SERVER
    private void Awake() {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        UpdateKeys();
    }
#endif

    public void UpdateKeys() {
        texteDash.text = "You can jump using ''" + InputManager.GetBindingName("Jump", 0) + "'' and dash with ''" + InputManager.GetBindingName("Dash", 0) + "''";
        texteSpell.text = "Use your skills with ''" + (InputManager.GetBindingName("LeftClick", 0) == "LMB" ? "Left mouse click" : InputManager.GetBindingName("LeftClick", 0)) + "'', ''" + InputManager.GetBindingName("Spell1", 0) + "'', " + InputManager.GetBindingName("Spell2", 0) + "'', " + InputManager.GetBindingName("Spell3", 0) + "'', " + InputManager.GetBindingName("Spell4", 0) + "''";
        texteSpell2.text = "Use your skills with ''" + InputManager.GetBindingName("LeftClick", 0) + "'', ''" + InputManager.GetBindingName("Spell1", 0) + "'', " + InputManager.GetBindingName("Spell2", 0) + "'', " + InputManager.GetBindingName("Spell3", 0) + "'', " + InputManager.GetBindingName("Spell4", 0) + "''";
        texteMove.text = "You can move using ''" + InputManager.GetBindingName("Up", 0) + "'', ''" + InputManager.GetBindingName("Left", 0) + "'', " + InputManager.GetBindingName("Down", 0) + "'', " + InputManager.GetBindingName("Right", 0) + "''";
        texteTransform.text = "Transform into a spirit using ''" + InputManager.GetBindingName("Reincarnation", 0) + "''";

    }
}

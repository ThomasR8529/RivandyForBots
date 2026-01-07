using System;
using UnityEngine;
using UnityEngine.UI;



[Serializable]
public class ClassUIicon : MonoBehaviour
{

    // A remplacer par button Transition
    public RawImage selectedBg;

    bool locked = false;
    public void HoverCharacter()
    {
        if (locked) return;
        // Faire une action button transtion

    }

    public void NotHoverCharacter()
    {
        if (locked) return;
        // Faire une action button transtion
    }

    public void SetLocked(bool locking)
    {
        locked = locking;
    }
}

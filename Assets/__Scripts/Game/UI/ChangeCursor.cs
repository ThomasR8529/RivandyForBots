using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeCursor : MonoBehaviour
{
    public Texture2D cursorNormal;
    public Texture2D cursorImportant;
    public Texture2D cursorImpossible;
    public Texture2D cursorGreen;

    public List<ClassUIicon> classIcons;
    public List<Animator> classAnimators;
    private void Start()
    {
#if !UNITY_EDITOR
        Cursor.SetCursor(cursorNormal, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
#endif
    }
    public void SetCursor(int state)
    {
        Cursor.SetCursor(state == 0 ? cursorNormal : state == 1 ? cursorImportant : state == 2 ? cursorImpossible : state == 3 ? cursorGreen : state == 4 ? cursorGreen : cursorNormal, Vector2.zero, CursorMode.Auto);
        // Cursor.SetCursor(state == 0 ? cursorNormal : state == 1 ? cursorImportant : state == 2 ? cursorImpossible : state == 3 ? cursorGreen : state == 4 ? cursorGreen : cursorNormal, new Vector2(1, 1), CursorMode.ForceSoftware);
    }

    public void LockClass(int lockClassId)
    {
/*        foreach (ClassUIicon classIcon in classIcons)
        {
            if (lockClassId == classIcon.id)
            {
                classIcon.SetLocked(true);
*//*                classIcon.selectedBg.color = classIcon.lockColor;*//*
            }
            else
            {
                classIcon.SetLocked(false);
*//*                classIcon.selectedBg.color = classIcon.couleurGris;*//*
            }
        }*/
    }

    public void PlayBeginner(int classId)
    {
        classAnimators[classId].Play("beginner");
        
    }
}

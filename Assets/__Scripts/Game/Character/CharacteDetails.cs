using Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacteDetails : MonoBehaviour
{
    public string characterName;
    public Spell[] autoAttacks;
    public Spell[] spells;

    public CharacterSkin characterSkin;

    void Awake()
    {
        characterSkin = GetComponent<CharacterSkin>();
    }

}

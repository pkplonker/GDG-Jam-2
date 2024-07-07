using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum CharacterType { SCOUT,PICKUP,TRAP_DISARM}
public class CharacterManager : MonoBehaviour
{

    CharacterType activeCharacter;

    public List<CharacterUIDat> allCharacters;
    public List<CharacterUIDat> availableCharacters;

    private void Awake()
    {
        activeCharacter = CharacterType.SCOUT;
    }

    public void SetActiveCharacter(CharacterType ch)
    {
        activeCharacter = ch;
    }
}

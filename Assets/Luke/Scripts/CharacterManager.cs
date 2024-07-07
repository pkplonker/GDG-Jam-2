using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum CharacterType { SCOUT,PICKUP,TRAP_DISARM}
public class CharacterManager : MonoBehaviour
{

    CharacterType activeCharacter;

    public List<CharacterUIDat> allCharacters;
    public List<CharacterUIDat> availableCharacters;

    public event Action OnFinishedMoving; 

    private void Awake()
    {
        activeCharacter = CharacterType.SCOUT;
    }

    public void SetActiveCharacter(CharacterType ch)
    {
        activeCharacter = ch;
    }

    public void MoveCurrentCharacter(List<Vector3> points)
    {

        Debug.Log("Moving Character");
        StartCoroutine(TestMoveChar());
    }

    private IEnumerator TestMoveChar()
    {
        yield return new WaitForSeconds(1);
        OnFinishedMoving?.Invoke();
    }
}

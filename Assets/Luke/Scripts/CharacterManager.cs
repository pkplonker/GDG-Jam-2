using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public enum CharacterType { SCOUT,PICKUP,TRAP_DISARM}

[System.Serializable]
public class ActiveCharacterData
{
    public GameObject characterObj;
    public Vector3 currentPosition;
    public CharacterType characterType;

    public int numKeys = 0;
    public int range;
}
public class CharacterManager : MonoBehaviour
{
    Vector2Int[] startPos = {   new Vector2Int(0, 1),
                                new Vector2Int(0, -1),
                                new Vector2Int(1, 0),
                                new Vector2Int(-1, 0),
                                new Vector2Int(1, 1),
                                new Vector2Int(-1, 1),
                                new Vector2Int(-1, -1),
                                new Vector2Int(1, -1)};

    public List<ActiveCharacterData> allActiveCharacters = new List<ActiveCharacterData>();
    public ActiveCharacterData currActiveCharacter;

    public List<CharacterUIDat> allCharacters;
    public List<CharacterUIDat> availableCharacters;

    public event Action OnFinishedMoving;

    public float moveSpeed;

    MapGenerator currMapGen;
    private void Awake()
    {
        MapGenerator.OnMapGenerated += GenerateCharacters;

    }

    private void GenerateCharacters(MapGenerator mapGen)
    {
        currMapGen = mapGen;

        for (int i = 0; i<allActiveCharacters.Count;i++)
        {
            Destroy(allActiveCharacters[i].characterObj);
        }
        allActiveCharacters.Clear();


        int spawnId = 0;
        foreach(CharacterUIDat dat in availableCharacters)
        {
            GameObject go = Instantiate(dat.characterPrefab, mapGen.startRoom.bounds.center + new Vector3(startPos[spawnId].x, startPos[spawnId].y,0),Quaternion.identity);

            allActiveCharacters.Add(new ActiveCharacterData() { characterObj = go, characterType = dat.type, currentPosition = go.transform.position, range = dat.range });

            spawnId++;
        }

        currActiveCharacter = allActiveCharacters[0];
    }

    public void SetActiveCharacter(CharacterType ch)
    {
        currActiveCharacter = allActiveCharacters.FirstOrDefault(x => x.characterType == ch);
    }

    public void MoveCurrentCharacter(List<Vector3> points)
    {

        Debug.Log("Moving Character");

        StartCoroutine(StartMove(points,1));

        //StartCoroutine(TestMoveChar());
    }



    private IEnumerator StartMove(List<Vector3> points, int moveID)
    {
        yield return new WaitForSeconds(moveSpeed);

        if(moveID == points.Count) OnFinishedMoving?.Invoke();
        else
        {
            currActiveCharacter.currentPosition = points[moveID];
            currActiveCharacter.characterObj.transform.position = points[moveID];

            //Do Check for tile in MapGenerator

            bool isKey = currMapGen.IsKey(points[moveID]);
            bool isTrap = currMapGen.IsTrap(points[moveID]);
            Debug.Log(isTrap);
            if(isKey && currActiveCharacter.characterType == CharacterType.PICKUP)
            {
                OnPickUpKey(currActiveCharacter);
            }
            else if(isTrap && !(currActiveCharacter.characterType == CharacterType.TRAP_DISARM || currActiveCharacter.characterType == CharacterType.SCOUT))
            {
                OnTrapActivated();
            }

            if(isTrap && currActiveCharacter.characterType == CharacterType.TRAP_DISARM)
            {
                OnTrapDisarmed(points[moveID],currActiveCharacter.range);
            }

            if(currActiveCharacter.numKeys > 0)
            {
                if (currMapGen.TryUseKey(points[moveID], currActiveCharacter.range)) OnUnlockRoom(currActiveCharacter);
            }


            StartCoroutine(StartMove(points,moveID+1));
        }

    }


    private void OnPickUpKey(ActiveCharacterData character)
    {
        character.numKeys++;

        //Play Sound
        Debug.Log("Picking Up Key");
    }
    private void OnUnlockRoom(ActiveCharacterData character)
    {
        character.numKeys--;

        Debug.Log("Unlocking Room");
    }

    private void OnTrapDisarmed(Vector3 position, int range)
    {

        Debug.Log("Trying to Disarm Trap");
        if (currMapGen.TryDisarm(position, range))
        {
            //Play Sound

            Debug.Log("Disarming Trap");
        }
    }

    private void OnTrapActivated()
    {
        //End The Game

        Debug.Log("Activated Trap");
    }

    private IEnumerator TestMoveChar()
    {
        yield return new WaitForSeconds(1);
        OnFinishedMoving?.Invoke();
    }
}

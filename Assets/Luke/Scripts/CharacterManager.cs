using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum CharacterType
{
	SCOUT,
	PICKUP,
	TRAP_DISARM,
	EXPLOSIVE_TRAP_DISARM
}

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
	Vector2Int[] startPos =
	{
		new Vector2Int(0, 1),
		new Vector2Int(0, -1),
		new Vector2Int(1, 0),
		new Vector2Int(-1, 0),
		new Vector2Int(1, 1),
		new Vector2Int(-1, 1),
		new Vector2Int(-1, -1),
		new Vector2Int(1, -1)
	};

	public List<ActiveCharacterData> allActiveCharacters = new List<ActiveCharacterData>();
	public ActiveCharacterData currActiveCharacter;

	public List<CharacterUIDat> allCharacters;
	public List<CharacterUIDat> availableCharacters;

	public event Action OnFinishedMoving;

	public float moveSpeed;

	MapGenerator currMapGen;
	FogOfWar fog;
	private bool hasWon;
	private bool hasLost;

	private void Awake()
	{
		fog = GameObject.Find("FogOfWar").GetComponent<FogOfWar>();
		MapGenerator.OnMapGenerated += GenerateCharacters;
	}

	private void OnDestroy()
	{
		MapGenerator.OnMapGenerated -= GenerateCharacters;
		if (allActiveCharacters.Any())
		{
			foreach (var e in allActiveCharacters)
			{
				if (e?.characterObj != null)
				{
					Destroy(e?.characterObj);
				}
			}
		}
	}

	private void GenerateCharacters(MapGenerator mapGen)
	{
		currMapGen = mapGen;

		for (int i = 0; i < allActiveCharacters.Count; i++)
		{
			Destroy(allActiveCharacters[i].characterObj);
		}

		allActiveCharacters.Clear();
		fog.characters.Clear();

		int spawnId = 0;
		foreach (CharacterUIDat dat in availableCharacters)
		{
			GameObject go = Instantiate(dat.characterPrefab,
				mapGen.startRoom.bounds.center + new Vector3(startPos[spawnId].x, startPos[spawnId].y, 0),
				Quaternion.identity);

			allActiveCharacters.Add(new ActiveCharacterData()
			{
				characterObj = go, characterType = dat.type, currentPosition = go.transform.position, range = dat.range
			});

			spawnId++;

			if (dat.type == CharacterType.SCOUT)
			{
				fog.scoutObject = go;
			}
			else fog.characters.Add(go);
		}

		currActiveCharacter = allActiveCharacters[0];
	}

	public void SetActiveCharacter(CharacterType ch)
	{
		currActiveCharacter = allActiveCharacters.FirstOrDefault(x => x.characterType == ch);
	}

	public void MoveCurrentCharacter(List<Vector3> points)
	{
		StartCoroutine(StartMove(points, 1));

		//StartCoroutine(TestMoveChar());
	}

	private IEnumerator StartMove(List<Vector3> points, int moveID)
	{
		yield return new WaitForSeconds(moveSpeed);

		if (moveID == points.Count) OnFinishedMoving?.Invoke();
		else
		{
			currActiveCharacter.currentPosition = points[moveID];
			currActiveCharacter.characterObj.transform.position = points[moveID];

			//Do Check for tile in MapGenerator
			var trap = currMapGen.IsTrap(points[moveID]);
			bool isTrap = trap != null;

			Debug.Log(isTrap);

			var isTrophy = currMapGen.IsTrophy(points[moveID]);
			if (isTrophy)
			{
				OnTrophy();
			}
			else
			{
				if (currActiveCharacter.characterType == CharacterType.PICKUP)
				{


					if (isTrap)
					{
						OnTrapActivated();

					}

					bool isKey = currMapGen.IsKey(points[moveID]);
					if (isKey && currActiveCharacter.characterType == CharacterType.PICKUP)
					{
						OnPickUpKey(currActiveCharacter);
					}
				}
				else if (isTrap && currActiveCharacter.characterType != CharacterType.SCOUT)
				{
					if (currActiveCharacter.characterType == CharacterType.PICKUP)
                    {
						OnTrapActivated();
                    }
					else if (trap.trapType == TrapType.Explosives &&
						currActiveCharacter.characterType != CharacterType.EXPLOSIVE_TRAP_DISARM)
					{ OnTrapActivated(); }

					else if (trap.trapType == TrapType.Trap &&
						currActiveCharacter.characterType != CharacterType.TRAP_DISARM)
					{ OnTrapActivated(); }


					//if (trap.trapType == TrapType.Explosives &&
					//    (currActiveCharacter.characterType != CharacterType.EXPLOSIVE_TRAP_DISARM ||
					//     (trap.trapType == TrapType.Trap &&
					//      currActiveCharacter.characterType != CharacterType.TRAP_DISARM)) ||
					//    currActiveCharacter.characterType == CharacterType.PICKUP)
					//{
					//	OnTrapActivated();
					//}
				}


				if (isTrap &&
				    ((currActiveCharacter.characterType == CharacterType.TRAP_DISARM &&
				      trap.trapType == TrapType.Trap) ||
				     (currActiveCharacter.characterType == CharacterType.EXPLOSIVE_TRAP_DISARM &&
				      trap.trapType == TrapType.Explosives)))
				{
					OnTrapDisarmed(points[moveID], currActiveCharacter.range);
				}

				if (currActiveCharacter.numKeys > 0)
				{
					if (currMapGen.TryUseKey(points[moveID], currActiveCharacter.range))
						OnUnlockRoom(currActiveCharacter);
				}
			}

			StartCoroutine(StartMove(points, moveID + 1));
		}
	}

	private void OnPickUpKey(ActiveCharacterData character)
	{
		character.numKeys++;

		//Play Sound
		AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.globalSoundList.pickup);
		Debug.Log("Picking Up Key");
	}

	private void OnUnlockRoom(ActiveCharacterData character)
	{
		character.numKeys--;
		AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.globalSoundList.unlockRoom);

		Debug.Log("Unlocking Room");
	}

	private void OnTrapDisarmed(Vector3 position, int range)
	{
		Debug.Log("Trying to Disarm Trap");
		AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.globalSoundList.disarmTrap);

		if (currMapGen.TryDisarm(position, range))
		{
			//Play Sound

			Debug.Log("Disarming Trap");
		}
	}

	private void OnTrapActivated()
	{
		Debug.Log("Activated Trap");
		if (!hasLost)
		{
			hasLost = true;
			//End The Game
			AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.globalSoundList.explosion);

			Debug.Log("Activated Trap");
			GameObject.Find("GameManager").GetComponent<GameOverlay>().OnLoseGame();
		}
	}

	private void OnTrophy()
	{
		if (!hasWon)
		{
			//End The Game - Win
			AudioManager.Instance.PlaySoundEffect(AudioManager.Instance.globalSoundList.pickup);

			Debug.Log("Win");
			hasWon = true;

			GameObject.Find("GameManager").GetComponent<GameOverlay>().OnWinGame();
		}
	}

	private IEnumerator TestMoveChar()
	{
		yield return new WaitForSeconds(1);
		OnFinishedMoving?.Invoke();
	}
}
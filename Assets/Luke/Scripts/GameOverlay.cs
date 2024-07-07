using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class GameOverlay : UIComponent
{
	MapGenerator mapGenerator;
	CharacterManager chManager;

	public RectTransform characterSelectionPanel;
	public GameObject characterSelectionSingle;
	public TextMeshProUGUI moveCommandText;

	public CharacterSelectionSingle activeCharacter;
	public List<CharacterSelectionSingle> allCharacters = new List<CharacterSelectionSingle>();

	[SerializeField]
	private SceneSeeds sceneSeeds;

	bool generated;
	bool moving;

	Vector3 currMousePos;

	Camera mainCam;

	bool traversable;

	public Color pathCol;
	LineRenderer lr;
	Vector2 startPos;

	protected override void Awake()
	{
		base.Awake();

		mainCam = Camera.main;

		chManager = GetComponent<CharacterManager>();
		endScreen.SetActive(false);
		MapGenerator.OnMapGenerated += OnMapFinishedGenerating;
	}

	public void OnMapFinishedGenerating(MapGenerator mapGen)
	{
		mapGenerator = mapGen;
		DisplayComponent(this, true);
		GenerateUI();
		moveCommandText.gameObject.SetActive(false);
		StartCoroutine(MovingTextChange());
		generated = true;

		chManager.OnFinishedMoving += SetAbleToMove;

		lr = CreateLr(startPos);
	}

	private void OnDestroy()
	{
		if (chManager != null)
		{
			chManager.OnFinishedMoving -= SetAbleToMove;
		}

		MapGenerator.OnMapGenerated -= OnMapFinishedGenerating;
	}

	private void Update()
	{
		if (generated && !EventSystem.current.IsPointerOverGameObject())
		{
			currMousePos =
				mainCam.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, -mainCam.transform.position.z));

			var points = MapGenerator.CalculatePath(chManager.currActiveCharacter.currentPosition, currMousePos);

			traversable = (points != null);

			if (traversable && !moving)
			{
				//startPos = new Vector2(chManager.currActiveCharacter.currentPosition.x, chManager.currActiveCharacter.currentPosition.y);
				//Draw Line Points

				points.Insert(0, chManager.currActiveCharacter.currentPosition);

				//Debug.Log("Traversable");
				lr.positionCount = points.Count;
				lr.SetPositions(points.ToArray());

				if (Input.GetMouseButtonDown(0))
				{
					//Set waypoint
					moving = true;
					chManager.MoveCurrentCharacter(points);
					moveCommandText.gameObject.SetActive(true);

					foreach (CharacterSelectionSingle cs in allCharacters)
					{
						cs.onSelectButton.interactable = false;
					}
				}
			}
			else
			{
				//Debug.Log("Not Traversable");
				lr.positionCount = 0;
				lr.SetPositions(new Vector3[0]);
			}
		}
		else
		{
			if (lr != null)
			{
				lr.positionCount = 0;
				lr.SetPositions(new Vector3[0]);
			}
		}
	}

	private IEnumerator MovingTextChange()
	{
		yield return new WaitForSeconds(0.5f);
		moveCommandText.text += ".";

		if (moveCommandText.text == "Executing Move Command....") moveCommandText.text = "Executing Move Command";
		StartCoroutine(MovingTextChange());
	}

	private void SetAbleToMove()
	{
		moving = false;
		moveCommandText.gameObject.SetActive(false);

		foreach (CharacterSelectionSingle cs in allCharacters)
		{
			cs.onSelectButton.interactable = true;
		}
	}

	private void GenerateUI()
	{
		for (int i = 0; i < characterSelectionPanel.childCount; i++)
		{
			Destroy(characterSelectionPanel.GetChild(i).gameObject);
		}

		for (int i = 0; i < chManager.availableCharacters.Count; i++)
		{
			var currCharacter = chManager.availableCharacters[i];

			GameObject go = Instantiate(characterSelectionSingle, characterSelectionPanel);

			go.GetComponent<CharacterSelectionSingle>().SetCharacter(currCharacter, OnSelectCharacter);

			allCharacters.Add(go.GetComponent<CharacterSelectionSingle>());
		}

		OnSelectCharacter(allCharacters[0]);
	}

	private void OnSelectCharacter(CharacterSelectionSingle curr)
	{
		if (!moving)
		{
			Debug.Log("Selected: " + curr.thisDat.chName);
			chManager.SetActiveCharacter(curr.thisDat.type);

			foreach (CharacterSelectionSingle cs in allCharacters)
			{
				cs.panelBG.color = new Color(0.8f, 0.8f, 0.8f);
			}

			switch (curr.thisDat.type)
			{
				case CharacterType.SCOUT:
					curr.panelBG.color = Color.green;
					break;
				case CharacterType.PICKUP:
					curr.panelBG.color = Color.red;
					break;
				case CharacterType.TRAP_DISARM:
					curr.panelBG.color = new Color(186f / 255, 85f / 255, 211f / 255, 255);
					break;
				case CharacterType.EXPLOSIVE_TRAP_DISARM:
					curr.panelBG.color = new Color(1, 165f / 255, 0, 255);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private LineRenderer CreateLr(Vector2 pos)
	{
		var go = new GameObject();
		go.name = "Lr";
		go.transform.SetParent(transform);
		go.transform.position = new Vector3(pos.x + 1f, pos.y + 1f, 0);
		var lr = go.AddComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = pathCol;
		lr.endColor = pathCol;
		lr.widthMultiplier = 0.2f;
		lr.numCapVertices = 8;
		lr.useWorldSpace = true;
		lr.sortingOrder = 20;
		return lr;
	}

	public TextMeshProUGUI endTitle;
	public TextMeshProUGUI endDesc;
	public GameObject endScreen;

	public void OnLoseGame()
	{
		endScreen.SetActive(true);

		endDesc.text = "You triggered one of the traps!";
		endTitle.text = "GAME OVER!";
	}

	public void OnWinGame()
	{
		endScreen.SetActive(true);

		endDesc.text = "You successful finished the heist!";
		endTitle.text = "YOU WIN!";
	}

	public void ReturnToMain()
	{
		SceneManager.LoadScene("MainMenu");
	}

	public void PlayAgain()
	{
		var currentScene = sceneSeeds.currentSeed;
		do
		{
			sceneSeeds.currentSeed = sceneSeeds.availableSeeds[Random.Range(0, sceneSeeds.availableSeeds.Count)];
		} while (sceneSeeds.currentSeed != currentScene);

		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
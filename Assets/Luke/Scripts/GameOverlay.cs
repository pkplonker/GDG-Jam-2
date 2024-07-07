using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameOverlay : UIComponent
{
    MapGenerator mapGenerator;
    CharacterManager chManager;

    public RectTransform characterSelectionPanel;
    public GameObject characterSelectionSingle;


    public CharacterSelectionSingle activeCharacter;


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
        MapGenerator.OnMapGenerated += OnMapFinishedGenerating;
    }

    public void OnMapFinishedGenerating(MapGenerator mapGen)
    {
        mapGenerator = mapGen;
        DisplayComponent(this,true);
        GenerateUI();
        generated = true;

        chManager.OnFinishedMoving += SetAbleToMove;

        lr = CreateLr(startPos);
    }

    private void Update()
    {
        if (generated && !EventSystem.current.IsPointerOverGameObject())
        {

            currMousePos = mainCam.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, -mainCam.transform.position.z));

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
            lr.positionCount = 0;
            lr.SetPositions(new Vector3[0]);
        }
    }

    private void SetAbleToMove()
    {
        moving = false;
    }

    private void GenerateUI()
    {

        for(int i = 0; i < characterSelectionPanel.childCount;i++)
        {
            Destroy(characterSelectionPanel.GetChild(i).gameObject);
        }

        for (int i = 0; i<chManager.availableCharacters.Count; i++)
        {
            var currCharacter = chManager.availableCharacters[i];

            GameObject go = Instantiate(characterSelectionSingle, characterSelectionPanel);

            go.GetComponent<CharacterSelectionSingle>().SetCharacter(currCharacter,OnSelectCharacter);

        }
    }

    private void OnSelectCharacter(CharacterUIDat dat)
    {
        if (!moving)
        {
            Debug.Log("Selected: " + dat.chName);
            chManager.SetActiveCharacter(dat.type);
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
}

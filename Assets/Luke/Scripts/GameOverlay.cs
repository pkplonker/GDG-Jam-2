using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverlay : UIComponent
{
    CharacterManager chManager;

    public RectTransform characterSelectionPanel;
    public GameObject characterSelectionSingle;


    public CharacterSelectionSingle activeCharacter;

    protected override void Awake()
    {
        base.Awake();
        chManager = GetComponent<CharacterManager>();
        GenerateUI();
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
        Debug.Log("Selected: " + dat.chName);
        chManager.SetActiveCharacter(dat.type);
    }
}

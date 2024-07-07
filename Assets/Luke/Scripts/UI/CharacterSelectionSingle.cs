using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class CharacterSelectionSingle : MonoBehaviour
{
    public Image chImage;
    public TextMeshProUGUI chName;
    public TextMeshProUGUI chDesc;

    public Button onSelectButton;

    private CharacterUIDat thisDat;

    public void SetCharacter(CharacterUIDat dat, Action<CharacterUIDat> OnSelect)
    {
        thisDat = dat;

        chName.text = thisDat.chName;
        chDesc.text = thisDat.chDesc;
        chImage.sprite = thisDat.chImage;

        onSelectButton.onClick.AddListener(() => OnSelect(thisDat));
    }
}

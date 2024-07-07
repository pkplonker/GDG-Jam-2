using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class CharacterSelectionSingle : MonoBehaviour
{
    public Image panelBG;
    public Image chImage;
    public TextMeshProUGUI chName;
    public TextMeshProUGUI chDesc;

    public Button onSelectButton;

    public CharacterUIDat thisDat;

    public void SetCharacter(CharacterUIDat dat, Action<CharacterSelectionSingle> OnSelect)
    {
        thisDat = dat;

        chName.text = thisDat.chName;
        chDesc.text = thisDat.chDesc;
        chImage.sprite = thisDat.chImage;

        onSelectButton.onClick.AddListener(() => OnSelect(this));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CharacterUIData", order = 1)]
public class CharacterUIDat : ScriptableObject
{
    public CharacterType type;
    public string chName;
    public string chDesc;
    public Sprite chImage;
}

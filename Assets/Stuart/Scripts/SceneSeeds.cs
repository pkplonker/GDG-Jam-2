using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Seeds", menuName = "ScriptableObjects/Seeds", order = 2)]

public class SceneSeeds : ScriptableObject
{
    public List<int> availableSeeds;
    public int currentSeed;
}

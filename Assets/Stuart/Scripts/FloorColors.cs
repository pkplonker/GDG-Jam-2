using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Colors", menuName = "ScriptableObjects/Colors", order = 1)]

public class FloorColors : ScriptableObject
{
    public Color Floor;
    public Color LockedFloor;
    public Color Corridor;
    public Color Trap;
    public Color ExplosiveTrap;

}

using System;
using UnityEngine;

[Serializable]
public class BSPArgs
{
	public BoundsInt MapSize;
	public int MinWidth;
	public int MinHeight;
	public float HorizontalSplitChance;
	public bool RandomsizeSplit;
	public int RoomCountLimit;
	public Vector3Int Offset;
	public int Seed;

	public BSPArgs(BoundsInt mapSize, int minWidth, int minHeight, float horizontalSplitChance, bool randomsizeSplit, int roomCountLimit, Vector3Int offset, int seed)
	{
		MapSize = mapSize;
		MinWidth = minWidth;
		MinHeight = minHeight;
		HorizontalSplitChance = horizontalSplitChance;
		RandomsizeSplit = randomsizeSplit;
		RoomCountLimit = roomCountLimit;
		Offset = offset;
		Seed = seed;

	}

}
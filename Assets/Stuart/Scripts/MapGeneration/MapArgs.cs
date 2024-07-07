using System;
using UnityEngine;

[Serializable]
public class MapArgs
{
	public BoundsInt Bounds;
	public int MinWidth;
	public int MinHeight;
	public float HorizontalSplitChance;
	public bool RandomsizeSplit;
	public int RoomCountLimit;
	public Vector3Int Offset;
	public int Seed;
	public float RoomSeperation;

	public MapArgs(BoundsInt bounds, int minWidth, int minHeight, float horizontalSplitChance, bool randomsizeSplit, int roomCountLimit, Vector3Int offset, int seed)
	{
		Bounds = bounds;
		MinWidth = minWidth;
		MinHeight = minHeight;
		HorizontalSplitChance = horizontalSplitChance;
		RandomsizeSplit = randomsizeSplit;
		RoomCountLimit = roomCountLimit;
		Offset = offset;
		Seed = seed;

	}

}
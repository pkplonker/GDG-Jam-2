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
	public int Seed => seeds.currentSeed;
	public float RoomSeperation;
	public SceneSeeds seeds;
	public MapArgs(BoundsInt bounds, int minWidth, int minHeight, float horizontalSplitChance, bool randomsizeSplit, int roomCountLimit, Vector3Int offset)
	{
		Bounds = bounds;
		MinWidth = minWidth;
		MinHeight = minHeight;
		HorizontalSplitChance = horizontalSplitChance;
		RandomsizeSplit = randomsizeSplit;
		RoomCountLimit = roomCountLimit;
		Offset = offset;

	}

}
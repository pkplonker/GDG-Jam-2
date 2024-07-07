using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class Extensions
{
	private static Random rng = new();

	public static IList<T> Shuffle<T>(this IList<T> list, int seed)
	{
		rng = new Random(seed);
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}

		return list;
	}

	public static Vector3 ToV3(this Vector2Int position)
	{
		return new Vector3(position.x, position.y, 0);
	}
	public static Vector3 ToV3(this Vector3Int position)
	{
		return new Vector3(position.x, position.y, 0);
	}
	
	public static Vector3Int ToV3Int(this Vector2Int position)
	{
		return new Vector3Int(position.x, position.y, 0);
	}
	public static Vector2Int V2Int(this Vector3 position)
	{
		return new Vector2Int(Mathf.RoundToInt(position.x),Mathf.RoundToInt(position.y));
	}
	
}
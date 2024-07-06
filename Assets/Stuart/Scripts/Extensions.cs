﻿using System;
using System.Collections.Generic;

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
}
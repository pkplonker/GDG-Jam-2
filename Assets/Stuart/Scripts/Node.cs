using System;
using UnityEngine;

[Serializable]
public class Node
{
	public Vector2Int position => new Vector2Int(x, y);
	public int x;
	public int y;
	public bool walkable;
	public float f => g + h;

	public float g; //distance from start
	public float h; //estimated distance from end
	public Node parent = null;
	public int cost =0;
	public BoundsInt bounds;

	public Node(int x, int y, bool walkable)
	{
		this.x = x;
		this.y = y;
		this.walkable = walkable;
	}

	public Node(BoundsInt bounds)
	{
		this.bounds = bounds;
		x = (int) bounds.center.x;
		y = (int) bounds.center.y;
	}

	public override string ToString() => $"Node [{x}:{y}] - f{f}, g{g}, h{h}";

	public void Clear()
	{
		g = 0;
		h = 0;
		parent = null;
	}
}
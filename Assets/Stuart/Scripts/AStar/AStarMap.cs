using System.Collections.Generic;
using UnityEngine;

public class AStarMap : MonoBehaviour
{
	public Node[,] map;
	public float nodeSize;
	private int mapWidth;
	private int mapHeight;
	public bool debug;
	[SerializeField]
	private bool showMapGenDebug;

	public void GenerateMapData(MapArgs args, List<Room> rooms)
	{
		mapWidth = (int) (args.Bounds.size.x / nodeSize);
		mapHeight = (int) (args.Bounds.size.y / nodeSize);
		map = new Node[mapWidth, mapHeight];
		for (var z = 0; z < mapHeight; z++)
		{
			for (var x = 0; x < mapWidth; x++)
			{
				var rayDistance = 10f;
				map[x, z] = new Node(x, z, true);
			}
		}

		foreach (var room in rooms)
		{
			for (int x = 0; x < room.bounds.size.x; x++)
			{
				for (int y = 0; y < room.bounds.size.y; y++)
				{
					map[room.bounds.min.x + x, room.bounds.min.y + y].cost = 10;
				}
			}
		}
	}

	public Vector3 GetCellLocationFromIndex(int x, int z)
	{
		var bottomLeft = GetBottomLeft();
		var pos = new Vector3(bottomLeft.x + (x * nodeSize) + nodeSize / 2,
			bottomLeft.y + (z * nodeSize) + nodeSize / 2, 0);
		return pos;
	}

	public Vector3 GetCellLocationFromNode(Node node) => GetCellLocationFromIndex(node.x, node.y);

	private Vector2 GetBottomLeft() => new(transform.position.x - ((float) mapWidth / 2 * nodeSize),
		transform.position.y - ((float) mapHeight / 2 * nodeSize));

	public Node GetNodeFromLocation(Vector3 location)
	{
		var bottomLeft = GetBottomLeft();
		if (location.x < bottomLeft.x || location.x > bottomLeft.x + (nodeSize * mapWidth))
		{
			Debug.Log("Outside of bounds X");
			return null;
		}

		if (location.y < bottomLeft.y || location.y > bottomLeft.y + (nodeSize * mapHeight))
		{
			Debug.Log("Outside of bounds Y");
			return null;
		}

		var xDistanceFromBL = bottomLeft.x < location.x ? bottomLeft.x - location.x : location.x - bottomLeft.x;
		var yDistanceFromBL = bottomLeft.y < location.y ? bottomLeft.y - location.y : location.y - bottomLeft.y;
		var x = Mathf.Abs(Mathf.RoundToInt(xDistanceFromBL / nodeSize));
		var y = Mathf.Abs(Mathf.RoundToInt(yDistanceFromBL / nodeSize));

		if (x >= mapWidth || y >= mapHeight)
		{
			Debug.LogWarning("Trying to access out of bounds");
			return null;
		}

		return map[x, y]; //update
	}

	private void OnDrawGizmos()
	{
		if (!debug) return;
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(transform.position, new Vector3(mapWidth, mapHeight, 0));
		if (map == null) return;
		for (var y = 0; y < mapHeight; y++)
		{
			for (var x = 0; x < mapWidth; x++)
			{
				if (showMapGenDebug)
				{
					if (map[x, y].cost >= 10)
					{
						Gizmos.color = Color.red;
					}
					else if (map[x, y].cost >=5)
					{
						Gizmos.color = Color.yellow;
					}
					else
					{
						Gizmos.color = Color.green;
					}
				}
				else
				{
					Gizmos.color = map[x, y].walkable ? Color.green: Color.black;
				}
				

				var loc = GetCellLocationFromIndex(x, y);
				Gizmos.DrawCube(loc,
					new Vector3(nodeSize - 0.1f, nodeSize - 0.1f, 0.1f));
			}
		}
	}

	public IEnumerable<Node> CalculateNeighbours(Node target)
	{
		var neighbours = new List<Node>();
		for (var y = -1; y < 2; y++)
		{
			for (var x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;
				var currentX = target.x + x;
				var currentY = target.y + y;
				if (currentX >= mapWidth || currentX < 0 || currentY >= mapWidth || currentY < 0) continue;
				neighbours.Add(map[currentX, currentY]);
			}
		}

		return neighbours;
	}

	public int GetNodeCount() => mapWidth * mapHeight;

	public void ClearNodes()
	{
		for (var z = 0; z < map.GetLength(1); z++)
		{
			for (var x = 0; x < map.GetLength(0); x++)
			{
				map[x, z].Clear();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AStarMap))]
public class MapGenerator : MonoBehaviour
{
	[SerializeField]
	private MapArgs MapArgs;

	[SerializeField]
	private List<BoundsInt> rooms;

	private BinarySpacePartition binarySpacePartition = new();

	[SerializeField]
	private List<BoundsInt> chosenRooms;

	[SerializeField]
	private List<BoundsInt> offsetRooms;

	[SerializeField]
	private List<Edge> edges;

	private static AStarMap aStarMap;
	private static AStar aStar;
	public static Node[,] MapData => aStarMap.map;

	private void Start()
	{
		Generate();
	}

	public static List<Vector3> CalculatePath(Vector3 start, Vector3 end) => aStar.CalculatePath(aStarMap, start, end);

	public void Generate()
	{
		Clear();
		binarySpacePartition = new();
		UnityEngine.Random.InitState(MapArgs.Seed);
		CreateRooms();
		edges = ConnectRooms(offsetRooms.Select(x => new Node(x)));

		aStarMap = GetComponent<AStarMap>();
		gameObject.transform.position = new Vector3(MapArgs.Bounds.size.x / 2, MapArgs.Bounds.size.y / 2, 0);
		aStarMap.GenerateMapData(MapArgs, edges, offsetRooms);

		aStar = GetComponent<AStar>();
		GenerateCorridors();

		GenerateFinalWalkableArea();

		Debug.Log("Generated");
	}

	private void GenerateCorridors()
	{
		var corridorPoints = new List<Vector3>();
		foreach (var edge in edges)
		{
			var elements = aStar.CalculatePath(aStarMap, edge.start.position, edge.end.position);
			corridorPoints.AddRange(elements ?? new List<Vector3>());
		}

		for (int i = 0; i < corridorPoints.Count - 1; i++)
		{
			var p1 = GetNodePos(corridorPoints, i);
			var n = aStarMap.GetNodeFromLocation(p1);

			var offset = 1.00f;
			var n2 = aStarMap.GetNodeFromLocation(p1 + new Vector3(0, offset, 0));
			if (Math.Abs(GetNodePos(corridorPoints, i + 1).x - p1.x) < float.Epsilon)
			{
				n2 = aStarMap.GetNodeFromLocation(p1 + new Vector3(offset, offset, 0));
			}

			if (n.cost < 5)
			{
				n.cost = 5;
			}

			if (n2.cost < 5)
			{
				n2.cost = 5;
			}
		}
	}

	private void GenerateFinalWalkableArea()
	{
		for (int x = 0; x < aStarMap.map.GetLength(0); x++)
		{
			for (int y = 0; y < aStarMap.map.GetLength(1); y++)
			{
				var node = aStarMap.map[x, y];
				if (node.cost != 0)
				{
					node.walkable = true;
				}
				else
				{
					node.walkable = false;
				}
			}
		}
	}

	private static Vector3 GetNodePos(List<Vector3> corridorPoints, int i)
	{
		var p1 = corridorPoints[i];
		p1 = new Vector3Int(Mathf.FloorToInt(p1.x), Mathf.FloorToInt(p1.y), Mathf.RoundToInt(p1.z));
		return p1;
	}

	private List<Edge> ConnectRooms(IEnumerable<Node> roomCentres)
	{
		var centres = new List<Node>();
		centres.AddRange(roomCentres);
		List<Edge> edges = new List<Edge>();
		var currentRoomCentre = roomCentres.OrderBy(x => x.position.x).FirstOrDefault() ?? throw new Exception();
		while (centres.Any())
		{
			var closest = centres.OrderBy(x => Vector2Int.Distance(x.position, currentRoomCentre.position))
				.FirstOrDefault();
			centres.Remove(closest);
			edges.Add(new Edge(currentRoomCentre, closest));
			currentRoomCentre = closest;
		}

		return edges;
	}

	private void CreateRooms()
	{
		rooms = binarySpacePartition.BinaryPartition(MapArgs);
		chosenRooms = GenerateRoomSubset(rooms);
		offsetRooms = GenerateOffsetRooms(chosenRooms);
	}

	private List<BoundsInt> GenerateOffsetRooms(List<BoundsInt> rooms)
	{
		var result = new List<BoundsInt>();
		for (int i = rooms.Count - 1; i >= 0; i--)
		{
			var newRoom = rooms[i];
			var newMin = newRoom.min + MapArgs.Offset;
			var newSize = newRoom.size - MapArgs.Offset;

			newRoom.size = newSize;
			newRoom.min = newMin;
			result.Add(newRoom);
		}

		return result;
	}

	private List<BoundsInt> GenerateRoomSubset(List<BoundsInt> rooms) =>
		rooms.Shuffle(MapArgs.Seed).ToList().GetRange(0, Mathf.Min(rooms.Count, MapArgs.RoomCountLimit));

	public void Clear()
	{
		rooms = new List<BoundsInt>();
		edges = new List<Edge>();
		chosenRooms = new List<BoundsInt>();
		offsetRooms = new List<BoundsInt>();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(MapArgs.Bounds.center, MapArgs.Bounds.size);

		Gizmos.color = Color.white;
		foreach (var room in offsetRooms)
		{
			Gizmos.DrawCube(room.center, room.size);
		}

		// Gizmos.color = Color.blue;
		// foreach (var room in rooms)
		// {
		// 	Gizmos.DrawWireCube(room.center, room.size);
		// }
		//
		// Gizmos.color = Color.yellow;
		// foreach (var room in chosenRooms)
		// {
		// 	Gizmos.DrawWireCube(room.center, room.size);
		// }

		Gizmos.color = Color.black;
		foreach (var room in offsetRooms)
		{
			Gizmos.DrawWireCube(room.center, room.size);
		}

		Gizmos.color = Color.magenta;
		foreach (var edge in edges)
		{
			Gizmos.DrawLine(edge.start.position.ToV3(), edge.end.position.ToV3());
		}
	}
}
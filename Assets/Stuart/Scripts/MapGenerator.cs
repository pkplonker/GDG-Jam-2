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

	public static AStarMap aStarMap;
	public static AStar aStar;
	public BoundsInt startRoom;
	public BoundsInt endRoom;

	[SerializeField]
	private List<BoundsInt> primaryRooms;

	[SerializeField]
	private List<BoundsInt> tertiaryRooms;

	[SerializeField]
	private bool debug;

	private List<Vector3> pathPoints;
	public static Node[,] MapData => aStarMap.map;
	public static event Action<MapGenerator> OnMapGenerated;

	private void Start()
	{
		Generate();
	}

	public static List<Vector3> CalculatePath(Vector3 start, Vector3 end) => aStar.CalculatePath(aStarMap, start, end);

	public void Generate()
	{
		Clear();

		aStarMap = GetComponent<AStarMap>();
		aStar = GetComponent<AStar>();
		binarySpacePartition = new();

		UnityEngine.Random.InitState(MapArgs.Seed);

		CreateRooms();

		var roomNodes = offsetRooms.Select(x => new Node(x));
		edges = ConnectRooms(roomNodes);

		gameObject.transform.position = new Vector3(MapArgs.Bounds.size.x / 2, MapArgs.Bounds.size.y / 2, 0);
		aStarMap.GenerateMapData(MapArgs, edges, offsetRooms);

		GenerateCorridors();
		GenerateFinalWalkableArea();

		primaryRooms = IdentifyPrimaryRooms(roomNodes);
		tertiaryRooms = offsetRooms.Except(primaryRooms).ToList();

		OnMapGenerated?.Invoke(this);
		Debug.Log("Generated");
	}

	private List<BoundsInt> IdentifyPrimaryRooms(IEnumerable<Node> roomNodes)
	{
		var result = new HashSet<BoundsInt>();

		pathPoints = CalculatePath(startRoom.center, endRoom.center);

		foreach (var p in pathPoints)
		{
			var gridPoint = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), 0);

			foreach (var rn in roomNodes)
			{
				if (IsPointInBounds(gridPoint, rn.bounds))
				{
					Debug.Log("Match");
					result.Add(rn.bounds);
				}
			}
		}

		Debug.Log("primary");
		return result.ToList();
	}

	private bool IsPointInBounds(Vector3Int point, BoundsInt bounds) =>
		point.x >= bounds.xMin && point.x <= bounds.xMax &&
		point.y >= bounds.yMin && point.y <= bounds.yMax;

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

			var xPos = n.x;
			var yPos = n.y;
			for (int x = -1; x < 1; x++)
			{
				for (int y = -1; y < 1; y++)
				{
					var node = aStarMap.map[xPos + x, yPos + y];
					if (node.cost < 5)
					{
						node.cost = 5;
					}
				}
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
		startRoom = currentRoomCentre.bounds;
		endRoom = roomCentres.OrderBy(x => x.position.x).LastOrDefault()?.bounds ?? throw new Exception();
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

	public List<BoundsInt> GenerateRoomSubset(List<BoundsInt> rooms)
	{
		List<BoundsInt> shuffledRooms = rooms.OrderBy(r => UnityEngine.Random.Range(0, 10000)).ToList();

		List<BoundsInt> selectedRooms = new List<BoundsInt>();
		int count = Mathf.Min(shuffledRooms.Count, MapArgs.RoomCountLimit);

		while (selectedRooms.Count < count && shuffledRooms.Count > 0)
		{
			BoundsInt room = shuffledRooms[0];
			shuffledRooms.RemoveAt(0);

			if (selectedRooms.Count == 0 || IsRoomFarEnough(room, selectedRooms, MapArgs.RoomSeperation))
			{
				selectedRooms.Add(room);
			}
		}

		return selectedRooms;
	}

	private bool IsRoomFarEnough(BoundsInt room, List<BoundsInt> selectedRooms, float minDistance = 10f)
	{
		foreach (var selectedRoom in selectedRooms)
		{
			if (Vector3.Distance(room.center, selectedRoom.center) < minDistance)
			{
				return false;
			}
		}

		return true;
	}

	public void Clear()
	{
		rooms = new List<BoundsInt>();
		edges = new List<Edge>();
		chosenRooms = new List<BoundsInt>();
		offsetRooms = new List<BoundsInt>();
		primaryRooms = new List<BoundsInt>();
		tertiaryRooms = new List<BoundsInt>();
		pathPoints = new List<Vector3>();
	}

	private void OnDrawGizmos()
	{
		if (!debug) return;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(MapArgs.Bounds.center, MapArgs.Bounds.size);

		foreach (var room in offsetRooms)
		{
			if (room.center == startRoom.center)
			{
				Gizmos.color = Color.cyan;
			}
			else if (room.center == endRoom.center)
			{
				Gizmos.color = Color.yellow;
			}
			else
			{
				Gizmos.color = Color.white;
			}

			Gizmos.DrawCube(room.center, room.size);
		}

		if (pathPoints != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLineStrip(new ReadOnlySpan<Vector3>(pathPoints.ToArray()), false);
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

		foreach (var r in primaryRooms)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(r.center, 1);
		}

		foreach (var r in tertiaryRooms)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(r.center, 1);
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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

	[SerializeField]
	private int dlaCycles;

	public static Node[,] MapData => aStarMap.map;
	public static event Action<MapGenerator> OnMapGenerated;
	private Dictionary<BoundsInt, HashSet<BoundsInt>> possibleKeyRoomsForPrimaryRoom;

	private List<Color> distinguishableColors = new()
	{
		Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan,
		new(1.0f, 0.5f, 0.0f),
		new(0.5f, 0.0f, 0.5f),
		new(0.0f, 0.5f, 0.5f),
		Color.gray
	};

	[SerializeField]
	private int debugIndex;

	private HashSet<BoundsInt> keyFindRooms;
	private HashSet<BoundsInt> keyUseRooms;

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
		aStarMap.GenerateMapData(MapArgs, offsetRooms);

		GenerateCorridors();
		GenerateFinalWalkableArea();

		primaryRooms = IdentifyPrimaryRooms(roomNodes);
		tertiaryRooms = offsetRooms.Except(primaryRooms).ToList();

		CreateRoomDictionaries(roomNodes);

		DLA();
		SpawnKeys();
		OnMapGenerated?.Invoke(this);
		Debug.Log("Generated");
	}

	private void SpawnKeys()
	{
		keyUseRooms = possibleKeyRoomsForPrimaryRoom.SelectMany(x => x.Value).ToHashSet();
		keyFindRooms = possibleKeyRoomsForPrimaryRoom.Select(x => x.Key).ToHashSet();
		
	}

	/// <summary>
	/// This isn't 100% accurate as it's using pathing and not graphing in a conventional sense
	/// So can miss rooms, but the inaccuracy will not cause an invalid result, only missed ones
	/// </summary>
	/// <param name="roomNodes"></param>
	private void CreateRoomDictionaries(IEnumerable<Node> roomNodes)
	{
		possibleKeyRoomsForPrimaryRoom = new Dictionary<BoundsInt, HashSet<BoundsInt>>();
		foreach (var pr in primaryRooms.Where(x => x.center != startRoom.center))
		{
			possibleKeyRoomsForPrimaryRoom.Add(pr, new HashSet<BoundsInt>());
		}

		foreach (var tertRoom in tertiaryRooms)
		{
			var path = CalculatePath(tertRoom.center, startRoom.center);
			var primaryRoomsOnRoute = GetRoomsFromPath(path, roomNodes);

			var primaryRoomsAfter = primaryRooms.Except(primaryRoomsOnRoute);
			foreach (var pr in primaryRoomsAfter)
			{
				possibleKeyRoomsForPrimaryRoom[pr].Add(tertRoom);
			}
		}

		possibleKeyRoomsForPrimaryRoom = possibleKeyRoomsForPrimaryRoom
			.Where(x => x.Value.Any())
			.ToDictionary(x => x.Key, x => x.Value);
	}

	private List<BoundsInt> IdentifyPrimaryRooms(IEnumerable<Node> roomNodes)
	{
		pathPoints = CalculatePath(startRoom.center, endRoom.center);

		return GetRoomsFromPath(pathPoints, roomNodes).ToList();
	}

	private HashSet<BoundsInt> GetRoomsFromPath(List<Vector3> pathPoints, IEnumerable<Node> roomNodes)
	{
		var result = new HashSet<BoundsInt>();
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

		return result;
	}

	private void DLA()
	{
		var maxX = MapArgs.Bounds.size.x - 1;
		var maxY = MapArgs.Bounds.size.y - 1;

		for (var i = 0; i < dlaCycles; i++)
		{
			var rx = UnityEngine.Random.Range(0, maxX);
			var ry = UnityEngine.Random.Range(0, maxY);
			var node = MapData[rx, ry];
			if (node.walkable) continue;
			ProcessDLANeigbour(rx, maxX, ry, maxY, node, -1, 0);
			ProcessDLANeigbour(rx, maxX, ry, maxY, node, 1, 0);
			ProcessDLANeigbour(rx, maxX, ry, maxY, node, 0, -1);
			ProcessDLANeigbour(rx, maxX, ry, maxY, node, 0, 1);
		}
	}

	private static void ProcessDLANeigbour(int rx, int maxX, int ry, int maxY, Node node, int x, int y)
	{
		var neighbourNode = CheckWalkable(rx, x, maxX, ry, y, maxY);
		if (neighbourNode?.walkable ?? false)
		{
			node.walkable = true;
		}
	}

	private static Node CheckWalkable(int rx, int x, int maxX, int ry, int y, int maxY)
	{
		if (rx + x < maxX && ry + y < maxY && rx + x >= 0 && ry + y >= 0)
		{
			return MapData[rx + x, ry + y];
		}

		return null;
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
		possibleKeyRoomsForPrimaryRoom = new Dictionary<BoundsInt, HashSet<BoundsInt>>();
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

		// Gizmos.color = Color.magenta;
		// foreach (var edge in edges)
		// {
		// 	Gizmos.DrawLine(edge.start.position.ToV3(), edge.end.position.ToV3());
		// }

		// foreach (var r in primaryRooms)
		// {
		// 	Gizmos.color = Color.green;
		// 	Gizmos.DrawSphere(r.center + new Vector3(1.5f, 1.5f, 0), 1);
		// }
		//
		// foreach (var r in tertiaryRooms)
		// {
		// 	Gizmos.color = Color.cyan;
		// 	Gizmos.DrawSphere(r.center + new Vector3(1.5f, 1.5f, 0), 1);
		// }

		if (possibleKeyRoomsForPrimaryRoom != null)
		{
			foreach (var pair in possibleKeyRoomsForPrimaryRoom)
			{
				Gizmos.color = GetColorFromHash(pair.Key.GetHashCode());
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(pair.Key.center, 1);
				foreach (var tert in pair.Value)
				{
					Gizmos.DrawLine(pair.Key.center, tert.center);
					Gizmos.color = Color.yellow;
					Gizmos.DrawSphere(tert.center, 1);
				}
			}
		}

		// if (keyUseRooms != null)
		// {
		// 	foreach (var r in keyUseRooms)
		// 	{
		// 		Gizmos.color = Color.red;
		// 		Gizmos.DrawSphere(r.center, 1);
		// 	}
		// }
		//
		// if (keyFindRooms != null)
		// {
		// 	foreach (var r in keyFindRooms)
		// 	{
		// 		Gizmos.color = Color.yellow;
		// 		Gizmos.DrawSphere(r.center, 1);
		// 	}
		// }
	}

	Color GetColorFromHash(int hash)
	{
		hash = Mathf.Abs(hash);

		int colorIndex = hash % distinguishableColors.Count;

		return distinguishableColors[colorIndex];
	}
}
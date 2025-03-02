using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AStarMap))]
public class MapGenerator : MonoBehaviour
{
	public MapArgs MapArgs;

	[SerializeField]
	private List<Room> rooms;

	private BinarySpacePartition binarySpacePartition = new();

	[SerializeField]
	private List<Room> chosenRooms;

	[SerializeField]
	private List<Room> offsetRooms;

	[SerializeField]
	private List<Edge> edges;

	public static AStarMap aStarMap;
	public static AStar aStar;
	public Room startRoom;
	public Room endRoom;

	private static List<Room> primaryRooms;

	[SerializeField]
	private List<Room> tertiaryRooms;

	[SerializeField]
	private bool debug;

	private List<Vector3> pathPoints;

	[SerializeField]
	private int dlaCycles;

	public static Node[,] MapData => aStarMap.map;
	public static event Action<MapGenerator> OnMapGenerated;
	private Dictionary<Room, HashSet<Room>> possibleKeyRoomsForPrimaryRoom;

	[SerializeField]
	private FloorColors floorColors;

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

	public HashSet<Room> keyFindRooms;
	private HashSet<Room> keyUseRooms;

	[SerializeField]
	private GameObject keyPrefab;

	[SerializeField]
	private GameObject trophyPrefab;

	[SerializeField]
	private int nunberOfTraps = 7;

	private List<Trap> traps;

	[SerializeField]
	private float closestTrapDistance = 12f;

	private void Start()
	{
		Generate();
	}

	public Trap IsTrap(Vector3 position, int range = 3)
	{
		var targetPos = position.V2Int();
		var maxX = MapData.GetLength(0);
		var maxY = MapData.GetLength(1);

		var halfSize = Mathf.Min(Mathf.FloorToInt((float) range / 2f), 1);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				var checkPosition = targetPos + new Vector2Int(x, y);
				if (checkPosition.x < 0 || checkPosition.y < 0 || checkPosition.y > maxY - 1 ||
				    checkPosition.x > maxX - 1)
				{
					continue;
				}

				var node = MapData[checkPosition.x, checkPosition.y];
				if (node.Trap != null) return node.Trap;
			}
		}

		return null;
	}

	public bool IsTrophy(Vector3 position, int range = 3)
	{
		var targetPos = position.V2Int();
		var maxX = MapData.GetLength(0);
		var maxY = MapData.GetLength(1);

		var halfSize = Mathf.Min(Mathf.FloorToInt((float) range / 2f), 1);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				var checkPosition = targetPos + new Vector2Int(x, y);
				if (checkPosition.x < 0 || checkPosition.y < 0 || checkPosition.y > maxY - 1 ||
				    checkPosition.x > maxX - 1)
				{
					continue;
				}

				var node = MapData[checkPosition.x, checkPosition.y];
				if (node.Trophy != false) return true;
			}
		}

		return false;
	}

	public bool IsRoom(Vector3 position, int range = 3)
	{
		return PerformActionOnCondition(position, range, checkPosition =>
		{
			var node = MapData[checkPosition.x, checkPosition.y];
			if (node != null)
			{
				if (node.IsRoom) return true;
			}

			return false;
		});
	}

	public bool IsCorridor(Vector3 position, int range = 3)
	{
		return PerformActionOnCondition(position, range, checkPosition =>
		{
			var node = MapData[checkPosition.x, checkPosition.y];
			if (node != null)
			{
				if (node.IsCorridor) return true;
			}

			return false;
		});
	}

	public bool IsLocked(Vector3 position, int range = 3)
	{
		return PerformActionOnCondition(position, range, checkPosition =>
		{
			var node = MapData[checkPosition.x, checkPosition.y];
			if (node != null)
			{
				if (node.IsLocked) return true;
			}

			return false;
		});
	}

	public bool IsKey(Vector3 position, int range = 3)
	{
		return PerformActionOnCondition(position, range, checkPosition =>
		{
			var node = MapData[checkPosition.x, checkPosition.y];
			if (node != null && node.Prop != null)
			{
				DestroyKey(MapData[checkPosition.x, checkPosition.y]);
				return true;
			}

			return false;
		});
	}

	private bool PerformActionOnCondition(Vector3 position, int range, Func<Vector2Int, bool> action)
	{
		var targetPos = position.V2Int();
		var maxX = MapData.GetLength(0);
		var maxY = MapData.GetLength(1);

		var halfSize = Mathf.Min(Mathf.FloorToInt((float) range / 2f), 1);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				var checkPosition = targetPos + new Vector2Int(x, y);
				if (checkPosition.x < 0 || checkPosition.y < 0 || checkPosition.y > maxY - 1 ||
				    checkPosition.x > maxX - 1)
				{
					continue;
				}

				var result = action.Invoke(checkPosition);
				if (result) return true;
			}
		}

		return false;
	}

	private void DestroyKey(Node node)
	{
		if (node != null && node.Prop != null)
		{
			Destroy(node.Prop.gameObject);
			node.Prop = null;
		}
	}

	/// <summary>
	/// return true if key is used and room is unlocked
	/// </summary>
	/// <param name="position"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool TryUseKey(Vector3 position, int range)
	{
		var v = position.V2Int();
		var maxX = MapData.GetLength(0);
		var maxY = MapData.GetLength(1);

		var halfSize = Mathf.Max(Mathf.FloorToInt((float) range / 2f), 1);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				var pos = v + new Vector2Int(x, y);
				if (pos.x < 0 || pos.y < 0 || pos.y > maxY - 1 || pos.x > maxX - 1)
				{
					continue;
				}

				var node = MapData[pos.x, pos.y];
				if (node != null && node.IsLocked)
				{
					return UnlockRoom(node);
				}
			}
		}

		return false;
	}

	private bool UnlockRoom(Node node)
	{
		foreach (var lockedRoom in primaryRooms.Where(x => x.Locked))
		{
			if (IsPointInBounds(node.position.ToV3Int(), lockedRoom.bounds))
			{
				lockedRoom.Locked = false;
				foreach (var n in lockedRoom.Nodes)
				{
					n.IsLocked = false;
					n.Floor.color = floorColors.Floor;
					n.walkable = true;
				}

				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// return true if trap is disarmed
	/// </summary>
	/// <param name="position"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool TryDisarm(Vector3 position, int range)
	{
		range = 3;
		var v = position.V2Int();
		var maxX = MapData.GetLength(0);
		var maxY = MapData.GetLength(1);

		var halfSize = Mathf.Max(Mathf.FloorToInt((float) range / 2f), 1);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				var pos = v + new Vector2Int(x, y);
				if (pos.x < 0 || pos.y < 0 || pos.y > maxY - 1 || pos.x > maxX - 1)
				{
					continue;
				}

				var node = MapData[pos.x, pos.y];
				if (node != null && node.IsTrap)
				{
					DisarmTrap(node.Trap);
				}
			}
		}

		return false;
	}

	private void DisarmTrap(Trap trap)
	{
		foreach (var n in trap.nodes)
		{
			n.IsTrap = false;
			n.Trap = null;
			n.Floor.color = floorColors.Corridor;
		}
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

		var roomNodes = offsetRooms.Select(x => new Node(x.bounds));
		edges = ConnectRooms(roomNodes);

		gameObject.transform.position = new Vector3(MapArgs.Bounds.size.x / 2, MapArgs.Bounds.size.y / 2, 0);
		aStarMap.GenerateMapData(MapArgs, offsetRooms);

		GenerateCorridors();
		GenerateFinalWalkableArea();

		primaryRooms = IdentifyPrimaryRooms(roomNodes);
		tertiaryRooms = offsetRooms.Except(primaryRooms).ToList();

		CreateRoomDictionaries(roomNodes);

		DLA();
		SetupKeyRooms();

		SetupTrophy();

		OnMapGenerated?.Invoke(this);
		SetupTraps();
		SetLockedRoomsToNonTraversable();
		Debug.Log("Generated");
	}

	private void SetupTrophy()
	{
		var location = endRoom.bounds.center;
		var go = Instantiate(trophyPrefab);
		go.transform.SetParent(transform);
		go.transform.position = location;
		var sr = go.GetComponent<SpriteRenderer>();
		sr.sortingOrder = 3;
		var position = location.V2Int();

		MapData[position.x, position.y].Trophy = sr;
	}

	private void SetupTraps()
	{
		var corridorNodes = new List<Node>();
		for (var x = 0; x < MapData.GetLength(0); x++)
		{
			for (var y = 0; y < MapData.GetLength(1); y++)
			{
				var node = MapData[x, y];
				if (node.IsCorridor)
				{
					corridorNodes.Add(node);
				}
			}
		}

		corridorNodes.Shuffle(MapArgs.Seed);
		traps = new List<Trap>();
		for (var i = 0; traps.Count <= nunberOfTraps && i < corridorNodes.Count; i++)
		{
			var candidate = corridorNodes[i];
			if (!traps.Any())
			{
				traps.Add(CreateTrap(candidate));
				continue;
			}

			bool addTrap = true;
			var startRoomPos = startRoom.bounds.center.V2Int();
			if (Vector2Int.Distance(candidate.position, startRoomPos) <
			    closestTrapDistance)
			{
				continue;
			}

			foreach (var existingTrap in traps)
			{
				if (Vector2Int.Distance(candidate.position, existingTrap.nodes.First().position) <
				    closestTrapDistance)
				{
					addTrap = false;
					break;
				}
			}

			if (addTrap)
			{
				traps.Add(CreateTrap(candidate));
			}
		}
	}

	private Trap CreateTrap(Node candidate)
	{
		var nodes = new List<Node>();
		for (var x = -1; x < 2; x++)
		{
			for (var y = -1; y < 2; y++)
			{
				if (x < 0 && y < 0 && x > MapData.GetLength(0) - 1 && y > MapData.GetLength(1) - 1)
				{
					continue;
				}

				var node = MapData[candidate.x + x, candidate.y + y];
				if (node.IsCorridor)
				{
					node.IsTrap = true;
					nodes.Add(node);
					if (node.Floor != null) { }
				}
			}
		}

		var t = new Trap(nodes);
		foreach (var n in t.nodes)
		{
			n.Trap = t;
			n.Floor.color = t.trapType == TrapType.Trap ? floorColors.Trap : floorColors.ExplosiveTrap;
		}

		return t;
	}

	private void SetLockedRoomsToNonTraversable()
	{
		foreach (var lockedRooms in primaryRooms.Where(x => x.Locked))
		{
			foreach (var node in lockedRooms.Nodes)
			{
				node.walkable = false;
			}
		}
	}

	private void SetupKeyRooms()
	{
		keyUseRooms = new HashSet<Room>();
		keyFindRooms = new HashSet<Room>();
		foreach (var (primaryRoom, tertRooms) in possibleKeyRoomsForPrimaryRoom)
		{
			foreach (var tert in tertRooms.ToList().Shuffle(MapArgs.Seed))
			{
				if (keyFindRooms.Add(tert))
				{
					keyUseRooms.Add(primaryRoom);
					keyFindRooms.Add(tert);
					break;
				}
			}
		}

		foreach (var room in keyUseRooms)
		{
			LockRoom(room);
		}

		foreach (var room in keyFindRooms)
		{
			SpawnKey(room);
		}
	}

	private void SpawnKey(Room room)
	{
		var go = Instantiate(keyPrefab);
		go.transform.SetParent(transform);
		go.transform.position = room.bounds.center;
		var sr = go.GetComponent<SpriteRenderer>();
		sr.sortingOrder = 3;
		var position = room.bounds.center.V2Int();

		MapData[position.x, position.y].Prop = sr;
	}

	private void LockRoom(Room room, bool setLock = true)
	{
		room.Locked = setLock;
		for (int x = 0; x < MapData.GetLength(0); x++)
		{
			for (int y = 0; y < MapData.GetLength(1); y++)
			{
				if (IsPointInBounds(MapData[x, y].position.ToV3Int(), room.bounds))
				{
					var node = MapData[x, y];
					node.IsLocked = setLock;
					if (node.Floor)
					{
						node.Floor.color = floorColors.LockedFloor;
					}
				}
			}
		}
	}

	/// <summary>
	/// This isn't 100% accurate as it's using pathing and not graphing in a conventional sense
	/// So can miss rooms, but the inaccuracy will not cause an invalid result, only missed ones
	/// </summary>
	/// <param name="roomNodes"></param>
	private void CreateRoomDictionaries(IEnumerable<Node> roomNodes)
	{
		possibleKeyRoomsForPrimaryRoom = new Dictionary<Room, HashSet<Room>>();
		foreach (var pr in primaryRooms.Where(x => x.bounds.center != startRoom.bounds.center))
		{
			possibleKeyRoomsForPrimaryRoom.Add(pr, new HashSet<Room>());
		}

		foreach (var tertRoom in tertiaryRooms)
		{
			var path = CalculatePath(tertRoom.bounds.center, startRoom.bounds.center);
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

	private List<Room> IdentifyPrimaryRooms(IEnumerable<Node> roomNodes)
	{
		pathPoints = CalculatePath(startRoom.bounds.center, endRoom.bounds.center);

		var result = new HashSet<Room>();
		foreach (var p in pathPoints)
		{
			var gridPoint = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), 0);

			foreach (var rn in roomNodes)
			{
				if (IsPointInBounds(gridPoint, rn.bounds))
				{
					var foundRoom = offsetRooms.FirstOrDefault(x => rn.bounds.center == x.bounds.center);
					if (foundRoom != null)
					{
						result.Add(foundRoom);
					}
				}
			}
		}

		return result.ToList();
	}

	private HashSet<Room> GetRoomsFromPath(List<Vector3> pathPoints, IEnumerable<Node> roomNodes)
	{
		var result = new HashSet<Room>();
		foreach (var p in pathPoints)
		{
			var gridPoint = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), 0);

			foreach (var rn in roomNodes)
			{
				if (IsPointInBounds(gridPoint, rn.bounds))
				{
					result.Add(new Room() {bounds = rn.bounds});
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
			neighbourNode.Room?.Nodes.Add(node);
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

	private static bool IsPointInBounds(Vector3Int point, BoundsInt bounds) =>
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
		startRoom = new Room() {bounds = currentRoomCentre.bounds};
		endRoom = new Room() {bounds = roomCentres.OrderBy(x => x.position.x).LastOrDefault().bounds};
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
		rooms = binarySpacePartition.BinaryPartition(MapArgs).Select(x => new Room() {bounds = x}).ToList();
		chosenRooms = GenerateRoomSubset(rooms);
		offsetRooms = GenerateOffsetRooms(chosenRooms);
	}

	private List<Room> GenerateOffsetRooms(List<Room> rooms)
	{
		var result = new List<Room>();
		for (int i = rooms.Count - 1; i >= 0; i--)
		{
			var newRoom = rooms[i];
			var newMin = newRoom.bounds.min + MapArgs.Offset;
			var newSize = newRoom.bounds.size - MapArgs.Offset;

			newRoom.bounds.size = newSize;
			newRoom.bounds.min = newMin;
			result.Add(newRoom);
		}

		return result;
	}

	private List<Room> GenerateRoomSubset(List<Room> rooms)
	{
		List<Room> shuffledRooms = rooms.OrderBy(r => UnityEngine.Random.Range(0, 10000)).ToList();

		List<Room> selectedRooms = new List<Room>();
		int count = Mathf.Min(shuffledRooms.Count, MapArgs.RoomCountLimit);

		while (selectedRooms.Count < count && shuffledRooms.Count > 0)
		{
			Room room = shuffledRooms[0];
			shuffledRooms.RemoveAt(0);

			if (selectedRooms.Count == 0 || IsRoomFarEnough(room, selectedRooms, MapArgs.RoomSeperation))
			{
				selectedRooms.Add(room);
			}
		}

		return selectedRooms;
	}

	private bool IsRoomFarEnough(Room room, List<Room> selectedRooms, float minDistance = 10f)
	{
		foreach (var selectedRoom in selectedRooms)
		{
			if (Vector3.Distance(room.bounds.center, selectedRoom.bounds.center) < minDistance)
			{
				return false;
			}
		}

		return true;
	}

	public void Clear()
	{
		rooms = new List<Room>();
		edges = new List<Edge>();
		chosenRooms = new List<Room>();
		offsetRooms = new List<Room>();
		primaryRooms = new List<Room>();
		tertiaryRooms = new List<Room>();
		pathPoints = new List<Vector3>();
		possibleKeyRoomsForPrimaryRoom = new Dictionary<Room, HashSet<Room>>();
		traps = new List<Trap>();
		foreach (var t in transform.GetComponentsInChildren<Transform>().Where(t => t != transform))
		{
			Destroy(t.gameObject);
		}
	}

	private void OnDrawGizmos()
	{
		if (!debug) return;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(MapArgs.Bounds.center, MapArgs.Bounds.size);

		foreach (var room in offsetRooms)
		{
			if (room.bounds.center == startRoom.bounds.center)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawCube(room.bounds.center, room.bounds.size);
			}
			else if (room.bounds.center == endRoom.bounds.center)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawCube(room.bounds.center, room.bounds.size);
			}
			// else
			// {
			// 	Gizmos.color = Color.white;
			// }
		}

		// if (pathPoints != null)
		// {
		// 	Gizmos.color = Color.cyan;
		// 	Gizmos.DrawLineStrip(new ReadOnlySpan<Vector3>(pathPoints.ToArray()), false);
		// }

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

		// Gizmos.color = Color.black;
		// foreach (var room in offsetRooms)
		// {
		// 	Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);
		// }

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

		// if (possibleKeyRoomsForPrimaryRoom != null)
		// {
		// 	foreach (var pair in possibleKeyRoomsForPrimaryRoom)
		// 	{
		// 		Gizmos.color = GetColorFromHash(pair.Key.GetHashCode());
		// 		Gizmos.color = Color.red;
		// 		Gizmos.DrawSphere(pair.Key.center, 1);
		// 		foreach (var tert in pair.Value)
		// 		{
		// 			Gizmos.DrawLine(pair.Key.center, tert.center);
		// 			Gizmos.color = Color.yellow;
		// 			Gizmos.DrawSphere(tert.center, 1);
		// 		}
		// 	}
		// }

		// if (keyUseRooms != null)
		// {
		// 	foreach (var r in keyUseRooms)
		// 	{
		// 		Gizmos.color = Color.red;
		// 		Gizmos.DrawSphere(r.bounds.center, 1);
		// 	}
		// }
		//
		// if (keyFindRooms != null)
		// {
		// 	foreach (var r in keyFindRooms)
		// 	{
		// 		Gizmos.color = Color.yellow;
		// 		Gizmos.DrawSphere(r.bounds.center, 1);
		// 	}
		// }

		// if (traps != null)
		// {
		// 	foreach (var trap in traps)
		// 	{
		// 		Gizmos.color = Color.red;
		// 		Gizmos.DrawSphere(trap.nodes.FirstOrDefault().position.ToV3(), 0.5f);
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
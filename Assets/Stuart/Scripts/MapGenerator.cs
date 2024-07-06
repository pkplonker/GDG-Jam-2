using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	[SerializeField]
	private BSPArgs MapArgs;

	[SerializeField]
	private List<BoundsInt> rooms;

	private BinarySpacePartition binarySpacePartition = new();

	[SerializeField]
	private List<BoundsInt> chosenRooms;

	[SerializeField]
	private List<BoundsInt> offsetRooms;

	[SerializeField]
	private List<Edge> edges;

	public void Generate()
	{
		Clear();
		binarySpacePartition = new();
		UnityEngine.Random.InitState(MapArgs.Seed);
		CreateRooms();
		edges = ConnectRooms(offsetRooms.Select(x => new Node(x)));
		Debug.Log("Generated");
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
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(MapArgs.MapSize.center, MapArgs.MapSize.size);

		Gizmos.color = Color.white;
		foreach (var room in rooms)
		{
			Gizmos.DrawCube(room.center, room.size);
		}

		Gizmos.color = Color.blue;
		foreach (var room in rooms)
		{
			Gizmos.DrawWireCube(room.center, room.size);
		}

		Gizmos.color = Color.yellow;
		foreach (var room in chosenRooms)
		{
			Gizmos.DrawWireCube(room.center, room.size);
		}

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

[Serializable]
public class Node
{
	public Vector2Int position;

	public Node(BoundsInt bounds)
	{
		position = (Vector2Int) Vector3Int.RoundToInt(bounds.center);
	}

	public Node() { }
}

[Serializable]
public class Edge
{
	public Node start;
	public Node end;

	public Edge(Node start, Node end)
	{
		this.start = start;
		this.end = end;
	}
}
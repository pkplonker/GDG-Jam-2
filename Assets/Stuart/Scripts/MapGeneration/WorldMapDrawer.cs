using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldMapDrawer : MonoBehaviour
{
	private MapGenerator mapGenerator;
	private List<Vector3> debugCentres = new();

	[SerializeField]
	private Sprite floorSprite;

	[SerializeField]
	private Color floorColor;

	private void Awake()
	{
		MapGenerator.OnMapGenerated += OnMapGenerated;
	}

	private void OnMapGenerated(MapGenerator mapGenerator)
	{
		this.mapGenerator = mapGenerator;
		GenerateMap();
	}

	private void GenerateMap()
	{
		foreach (var t in transform.GetComponentsInChildren<Transform>().Where(x => x != transform))
		{
			Destroy(t.gameObject);
		}

		var map = MapGenerator.aStarMap.map;
		for (var x = 0; x < MapGenerator.MapData.GetLength(0) - 1; x++)
		{
			for (var y = 0; y < MapGenerator.MapData.GetLength(1) - 1; y++)
			{
				var node = map[x, y];
				GenerateWall(map, x, y);
				if (node.walkable)
				{
					CreateFloorSprite(node);
				}
			}
		}
	}

	private void CreateFloorSprite(Node node)
	{
		var go = new GameObject();
		go.name = "Floor";
		go.transform.position = new Vector3(node.position.x + 0.5f, node.position.y + 0.5f, 0);
		go.transform.SetParent(transform);
		go.transform.localScale = new Vector3(100, 100, 0);
		var sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = floorSprite;
		sr.color = floorColor;
		node.Floor = sr;
	}

	private void GenerateWall(Node[,] map, int x, int y)
	{
		var node = map[x, y];
		var nodeSize = 1.0f;

		void CreateWall(Vector2 startPos, Vector2 endPos)
		{
			var positions = new List<Vector3>
			{
				new Vector3(startPos.x, startPos.y, 0),
				new Vector3(endPos.x, endPos.y, 0)
			};
			var lr = CreateLr(startPos);
			lr.positionCount = positions.Count;
			lr.SetPositions(positions.ToArray());
			debugCentres.Add(new Vector3(startPos.x, startPos.y, 0));
			node.Walls.Add(lr);
		}

		if (y < map.GetLength(1) - 1)
		{
			var upNode = map[x, y + 1];
			if (node.walkable != upNode.walkable)
			{
				CreateWall(node.position + new Vector2(0, nodeSize), node.position + new Vector2(nodeSize, nodeSize));
			}
		}

		if (x < map.GetLength(0) - 1)
		{
			var rightNode = map[x + 1, y];
			if (node.walkable != rightNode.walkable)
			{
				CreateWall(node.position + new Vector2(nodeSize, 0), node.position + new Vector2(nodeSize, nodeSize));
			}
		}

		if (node.walkable && x == 0)
		{
			CreateWall(node.position, node.position + new Vector2(0, nodeSize));
		}

		if (node.walkable && y == 0)
		{
			CreateWall(node.position, node.position + new Vector2(nodeSize, 0));
		}

		if (node.walkable && x == map.GetLength(0) - 1)
		{
			CreateWall(node.position + new Vector2(nodeSize, 0), node.position + new Vector2(nodeSize, nodeSize));
		}

		if (node.walkable && y == map.GetLength(1) - 1)
		{
			CreateWall(node.position + new Vector2(0, nodeSize), node.position + new Vector2(nodeSize, nodeSize));
		}
	}

	private void OnDrawGizmos()
	{
		// foreach (var position in debugCentres)
		// {
		// 	Gizmos.color = Color.blue;
		// 	Gizmos.DrawSphere(position, 0.1f);
		// }
	}

	private LineRenderer CreateLr(Vector2 pos)
	{
		var go = new GameObject();
		go.name = "Lr";
		go.transform.SetParent(transform);
		go.transform.position = new Vector3(pos.x + 1f, pos.y + 1f, 0);
		var lr = go.AddComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.widthMultiplier = 0.2f;
		lr.numCapVertices = 8;
		lr.useWorldSpace = true;
		lr.sortingOrder = 20;
		return lr;
	}
}
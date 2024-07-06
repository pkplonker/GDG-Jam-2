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
					CreateSprite(node);
				}
			}
		}
	}

	private void CreateSprite(Node node)
	{
		var go = new GameObject();
		go.name = "Floor";
		go.transform.position = new Vector3(node.position.x+0.5f, node.position.y+0.5f, 0);
		go.transform.SetParent(transform);
		go.transform.localScale = new Vector3(100, 100, 0);
		var sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = floorSprite;
		sr.color = floorColor;
	}

	private void GenerateWall(Node[,] map, int x, int y)
	{
		var node = map[x, y];
		var upNode = map[x, y + 1];
		var rightNode = map[x + 1, y];
		var nodeSize = 1.0f;
		if (node.walkable != upNode.walkable)
		{
			var pos = node.position + new Vector2(0, nodeSize);
			var positions = new List<Vector3>();
			var lr = CreateLr(pos);

			positions.Add(new Vector3(pos.x, pos.y, 0));
			positions.Add(new Vector3(pos.x + nodeSize, pos.y, 0));

			lr.positionCount = positions.Count;
			lr.SetPositions(positions.ToArray());
			debugCentres.Add(new Vector3(pos.x, pos.y, 0));
		}

		if (node.walkable != rightNode.walkable)
		{
			var pos = node.position + new Vector2(nodeSize, 0);
			var positions = new List<Vector3>();
			var lr = CreateLr(pos);

			positions.Add(new Vector3(pos.x, pos.y, 0));
			positions.Add(new Vector3(pos.x, pos.y + nodeSize, 0));

			lr.positionCount = positions.Count;
			lr.SetPositions(positions.ToArray());
			debugCentres.Add(new Vector3(pos.x, pos.y, 0));
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
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldMapDrawer : MonoBehaviour
{
	private MapGenerator mapGenerator;
	private List<Vector3> debugPositions = new();

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
		var map = MapGenerator.aStarMap.map;
		for (int x = 0; x < MapGenerator.MapData.GetLength(0) - 1; x++)
		{
			for (int y = 0; y < MapGenerator.MapData.GetLength(1) - 1; y++)
			{
				var node = map[x, y];
				var upNode = map[x, y + 1];
				var rightNode = map[x + 1, y];
				var nodeSize = 1.0f;
				var halfNodeSize = nodeSize / 2;
				if (node.walkable != upNode.walkable)
				{
					var lr = CreateLr();
					Vector2 delta = new Vector2((float) node.position.x - (float) upNode.position.x,
						(float) node.position.y - (float) upNode.position.y)/2;
					var pos = node.position + delta;
					var positions = new List<Vector3>();

					positions.Add(new Vector3(pos.x - halfNodeSize, pos.y+halfNodeSize, 0));
					positions.Add(new Vector3(pos.x + halfNodeSize, pos.y+halfNodeSize, 0));
					lr.positionCount = positions.Count;
					lr.SetPositions(positions.ToArray());
					debugPositions.AddRange(positions);
				}

				if (node.walkable != rightNode.walkable)
				{
					var lr = CreateLr();

					Vector2 delta = new Vector2((float) node.position.x - (float) rightNode.position.x,
						(float) node.position.y - (float) rightNode.position.y)/2;
					var pos = node.position;
					var positions = new List<Vector3>();

					positions.Add(new Vector3(pos.x+halfNodeSize, pos.y - halfNodeSize, 0));
					positions.Add(new Vector3(pos.x+halfNodeSize, pos.y + halfNodeSize, 0));
					lr.positionCount = positions.Count;
					lr.SetPositions(positions.ToArray());
					debugPositions.AddRange(positions);

				}
			}
		}
	}

	private void OnDrawGizmos()
	{
		foreach (var position in debugPositions)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(position, 0.1f);
		}
	}

	private LineRenderer CreateLr()
	{
		var go = new GameObject();
		go.transform.SetParent(transform);
		var lr = go.AddComponent<LineRenderer>();
		var width = 0.1f;
		lr.startWidth = width;
		lr.endWidth = width;
		return lr;
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldMapDrawer : MonoBehaviour
{
	private MapGenerator mapGenerator;
	private List<Vector3> debugCentres = new();

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
				if (node.walkable != upNode.walkable)
				{
					var lr = CreateLr();
					var pos = node.position + new Vector2(0,nodeSize);
					var positions = new List<Vector3>();
				
					positions.Add(new Vector3(pos.x, pos.y, 0));
					positions.Add(new Vector3(pos.x+nodeSize, pos.y, 0));
					
					lr.positionCount = positions.Count;
					lr.SetPositions(positions.ToArray());
					debugCentres.Add(new Vector3(pos.x, pos.y, 0));
				}

				if (node.walkable != rightNode.walkable)
				{
					var lr = CreateLr();

					var pos = node.position + new Vector2(nodeSize, 0);
					var positions = new List<Vector3>();

					positions.Add(new Vector3(pos.x, pos.y, 0));
					positions.Add(new Vector3(pos.x , pos.y + nodeSize, 0));

					lr.positionCount = positions.Count;
					lr.SetPositions(positions.ToArray());
					debugCentres.Add(new Vector3(pos.x, pos.y, 0));
				}
			}
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

	private LineRenderer CreateLr()
	{
		var go = new GameObject();
		go.transform.SetParent(transform);
		var lr = go.AddComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.widthMultiplier = 0.2f;
		lr.numCapVertices = 8;
		return lr;
	}
}
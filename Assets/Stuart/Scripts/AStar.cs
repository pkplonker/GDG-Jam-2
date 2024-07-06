using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStar : MonoBehaviour
{
	public AStarMap aStarMap { get; set; }
	public List<Node> open { get; private set; } = new();
	public Dictionary<Node, int> closed { get; private set; } = new();
	private Node startNode = null;
	private Node endNode = null;
	private void Awake() => aStarMap = GetComponent<AStarMap>();
	public List<Vector3> points { get; private set; } = new();
	public bool debug;

	private void OnDrawGizmos()
	{
		if (!debug) return;
		if (open.Count != 0)
		{
			foreach (var t in open)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(aStarMap.GetCellLocationFromNode(t), aStarMap.nodeSize * 0.4f);
			}
		}

		if (closed.Count != 0)
		{
			foreach (var t in closed)
			{
				Gizmos.color = Color.black;
				Gizmos.DrawSphere(aStarMap.GetCellLocationFromNode(t.Key), aStarMap.nodeSize * 0.4f);
			}
		}

		if (points.Count != 0)
		{
			foreach (var t in points)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(t, aStarMap.nodeSize * 0.4f);
			}
		}

		if (startNode != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(aStarMap.GetCellLocationFromIndex(startNode.x, startNode.y), aStarMap.nodeSize * 0.4f);
		}

		if (endNode != null)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(aStarMap.GetCellLocationFromIndex(endNode.x, endNode.y), aStarMap.nodeSize * 0.4f);
		}
	}

	public List<Vector3> CalculatePath(AStarMap aStarMap, Vector2Int start, Vector2Int end) =>
		CalculatePath(aStarMap, new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0));

	public List<Vector3> CalculatePath(AStarMap aStarMap, Vector3 start, Vector3 end)
	{
		aStarMap.ClearNodes();
		startNode = aStarMap.GetNodeFromLocation(start);
		endNode = aStarMap.GetNodeFromLocation(end);
		if (startNode == null)
		{
			Debug.LogError("Failed to get start node");
			return null;
		}

		if (endNode == null)
		{
			Debug.LogError("Failed to get end node");
			return null;
		}

		if (startNode == endNode)
		{
			Debug.Log("Already at destination");
			return null;
		}

		open = new();
		closed = new();
		open.Add(startNode);
		startNode.g = CalculateDistance(startNode, startNode) + startNode.cost; //dist from start
		startNode.h = CalculateDistance(startNode, endNode);
		while (open.Count > 0 && !closed.ContainsKey(endNode))
		{
			if (open.Count == 0)
			{
				Debug.Log("No more open nodes");
				break;
			}

			open = open.OrderBy(n => n.f).ToList();
			var currentNode = open[0];
			if (open.Contains(currentNode))
				open.Remove(currentNode);
			closed.Add(currentNode, 0);
			if (currentNode == endNode)
				return CalculateWaypoints(aStarMap, currentNode);
			foreach (var neighbour in aStarMap.CalculateNeighbours(currentNode))
			{
				if (!neighbour.walkable) continue;
				if (closed.ContainsKey(neighbour)) continue;
				var g = CalculateDistance(neighbour, currentNode) + currentNode.g; //dist from start
				var h = CalculateDistance(neighbour, endNode);
				//dist from end
				if (!open.Contains(neighbour))
				{
					neighbour.parent = currentNode;
					neighbour.g = g;
					neighbour.h = h;
					open.Add(neighbour);
				}
				else if (neighbour.f > g + h)
				{
					neighbour.parent = currentNode;
					neighbour.g = g;
					neighbour.h = h;
				}
			}
		}

		return CalculateWaypoints(aStarMap, endNode);
	}

	private float CalculateDistance(Node start, Node end)
	{
		var distanceX = Mathf.Abs(start.x - end.x);
		var distanceY = Mathf.Abs(start.y - end.y);

		if (distanceX > distanceY)
			return 1.41421f * distanceY + 1 * (distanceX - distanceY);
		return 1.41421f * distanceX + 1 * (distanceY - distanceX);
	}

	private List<Vector3> CalculateWaypoints(AStarMap aStarMap, Node node)
	{
		if (node.parent == null) return null;
		List<Vector3> waypoints = new();
		while (node.parent != null)
		{
			if (waypoints.Count > aStarMap.GetNodeCount())
			{
				Debug.LogError("in valid waypoint route");
				return null;
			}

			waypoints.Add(aStarMap.GetCellLocationFromIndex(node.x, node.y));
			node = node.parent;
		}

		waypoints.Reverse();
		points = waypoints;
		return waypoints;
	}
}
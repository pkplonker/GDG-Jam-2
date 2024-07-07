using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStar : MonoBehaviour
{
	public static AStarMap AStarMap { get; set; }
	public List<Node> open { get; private set; } = new();
	public Dictionary<Node, int> closed { get; private set; } = new();
	private Node startNode = null;
	private Node endNode = null;
	private void Awake() => AStarMap = GetComponent<AStarMap>();
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
				Gizmos.DrawSphere(AStarMap.GetCellLocationFromNode(t), AStarMap.nodeSize * 0.4f);
			}
		}

		if (closed.Count != 0)
		{
			foreach (var t in closed)
			{
				Gizmos.color = Color.black;
				Gizmos.DrawSphere(AStarMap.GetCellLocationFromNode(t.Key), AStarMap.nodeSize * 0.4f);
			}
		}

		if (points.Count != 0)
		{
			foreach (var t in points)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(t, AStarMap.nodeSize * 0.4f);
			}
		}

		if (startNode != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(AStarMap.GetCellLocationFromIndex(startNode.x, startNode.y), AStarMap.nodeSize * 0.4f);
		}

		if (endNode != null)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(AStarMap.GetCellLocationFromIndex(endNode.x, endNode.y), AStarMap.nodeSize * 0.4f);
		}
	}

	public List<Vector3> CalculatePath(AStarMap aStarMap, Vector2Int start, Vector2Int end) =>
		CalculatePath(aStarMap, new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0));

	public List<Vector3> CalculatePath(AStarMap aStarMap, Vector3 start, Vector3 end)
	{
		aStarMap.ClearNodes();
		Node startNode = aStarMap.GetNodeFromLocation(start);
		Node endNode = aStarMap.GetNodeFromLocation(end);

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

		List<Node> open = new List<Node>();
		Dictionary<Node, int> closed = new Dictionary<Node, int>();
		open.Add(startNode);
		startNode.g = CalculateDistance(startNode, startNode) + startNode.cost; //dist from start
		startNode.h = CalculateDistance(startNode, endNode);

		Node closestNode = startNode;
		float closestDistance = startNode.h;

		while (open.Count > 0)
		{
			open = open.OrderBy(n => n.f).ToList();
			Node currentNode = open[0];
			open.Remove(currentNode);
			closed.Add(currentNode, 0);

			if (currentNode == endNode)
				return CalculateWaypoints(aStarMap, currentNode);

			foreach (Node neighbour in aStarMap.CalculateNeighbours(currentNode))
			{
				if (!neighbour.walkable || closed.ContainsKey(neighbour)) continue;

				float g = CalculateDistance(neighbour, currentNode) + currentNode.g; //dist from start
				float h = CalculateDistance(neighbour, endNode); //dist from end

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

				if (h < closestDistance)
				{
					closestDistance = h;
					closestNode = neighbour;
				}
			}
		}

		return CalculateWaypoints(aStarMap, closestNode);
	}

	private float CalculateDistance(Node start, Node end)
	{
		float distanceX = Mathf.Abs(start.x - end.x);
		float distanceY = Mathf.Abs(start.y - end.y);

		if (distanceX > distanceY)
			return 1.41421f * distanceY + 1 * (distanceX - distanceY);
		return 1.41421f * distanceX + 1 * (distanceY - distanceX);
	}

	private List<Vector3> CalculateWaypoints(AStarMap aStarMap, Node node)
	{
		if (node.parent == null) return null;
		List<Vector3> waypoints = new List<Vector3>();
		while (node.parent != null)
		{
			if (waypoints.Count > aStarMap.GetNodeCount())
			{
				Debug.LogError("Invalid waypoint route");
				return null;
			}

			waypoints.Add(aStarMap.GetCellLocationFromIndex(node.x, node.y));
			node = node.parent;
		}

		waypoints.Reverse();
		return waypoints;
	}
}
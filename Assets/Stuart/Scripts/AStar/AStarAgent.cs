using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarAgent : MonoBehaviour
{
	[SerializeField] private Transform destination;
	private List<Vector3> waypoints;
	[SerializeField] private float speed = 3f;
	private Vector3 currentWaypointDestination = Vector3.positiveInfinity;
	private int currentWaypointIndex = 0;

	private void Update()
	{
		if (destination.position != currentWaypointDestination)
		{
			currentWaypointDestination = destination.position;
			waypoints = MapGenerator.CalculatePath(transform.position, destination.position);
			currentWaypointIndex = 0;
		}

		if (waypoints == null)
		{
			Debug.Log("No path");
			return;
		}

		if (currentWaypointIndex >= waypoints.Count)
		{
			Debug.Log("Reached destination");
			return;
		}

		if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex]) <= 0.1f)
			currentWaypointIndex++;
		transform.position =
			Vector3.MoveTowards(transform.position, waypoints[currentWaypointIndex], speed * Time.deltaTime);
	}
}
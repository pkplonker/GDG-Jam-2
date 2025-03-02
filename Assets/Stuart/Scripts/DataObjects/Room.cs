﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : IEquatable<Room>
{
	public BoundsInt bounds;
	public bool Locked;
	public List<Node> Nodes = new ();

	// public Room(List<Node> nodes)
	// {
	// 	
	// }
	private Vector3Int GetCenter() => bounds.position + (bounds.size / 2);

	public bool Equals(Room other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return GetCenter().Equals(other.GetCenter());
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((Room)obj);
	}

	public override int GetHashCode() => GetCenter().GetHashCode();
}
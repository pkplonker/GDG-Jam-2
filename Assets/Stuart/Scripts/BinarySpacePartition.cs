using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BinarySpacePartition
{
	public List<BoundsInt> BinaryPartition(MapArgs args)
	{
		var candidateQueue = new Queue<BoundsInt>();
		var candidateList = new List<BoundsInt>();
		candidateQueue.Enqueue(args.Bounds);
		while (candidateQueue.Any())
		{
			if (candidateQueue.Count > 50000)
			{
				Debug.LogError("Overflow");
				break;
			}
			var room = candidateQueue.Dequeue();
			if (room.size.y < args.MinHeight || room.size.x < args.MinWidth) continue;
			if (UnityEngine.Random.value > args.HorizontalSplitChance)
			{
				if (room.size.y >= args.MinHeight * 2)
				{
					SplitHorizontally(args, candidateQueue, room);
				}
				else if (room.size.x >= args.MinWidth * 2)
				{
					SplitVertically(args, candidateQueue, room);
				}
				else if (room.size.x >= args.MinWidth && room.size.y >= args.MinHeight)
				{
					candidateList.Add(room);
				}
			}
			else
			{
				if (room.size.x >= args.MinWidth * 2)
				{
					SplitVertically(args, candidateQueue, room);
				}
				else if (room.size.y >= args.MinHeight * 2)
				{
					SplitHorizontally(args, candidateQueue, room);
				}
				else if (room.size.x >= args.MinWidth && room.size.y >= args.MinHeight)
				{
					candidateList.Add(room);
				}
			}
		}

		return candidateList;
	}

	private void SplitVertically(MapArgs args, Queue<BoundsInt> candidatesQueue, BoundsInt room)
	{

		var split = args.RandomsizeSplit ? Random.Range(1, room.size.x) : room.size.x / 2;
		candidatesQueue.Enqueue(new BoundsInt(room.min, new Vector3Int(split, room.size.y, room.size.z)));
		candidatesQueue.Enqueue(new BoundsInt(new Vector3Int(room.min.x + split, room.min.y, room.min.z),
			new Vector3Int(room.size.x - split, room.size.y, room.size.z)));
	}

	private void SplitHorizontally(MapArgs args, Queue<BoundsInt> candidatesQueue, BoundsInt room)
	{
		var split = args.RandomsizeSplit ? Random.Range(1, room.size.y): room.size.y / 2;
		candidatesQueue.Enqueue(new BoundsInt(room.min, new Vector3Int(room.size.x, split, room.size.z)));
		candidatesQueue.Enqueue(new BoundsInt(new Vector3Int(room.min.x, room.min.y+split, room.min.z), new Vector3Int(room.size.x, room.size.y-split, room.size.z)));
	}
}


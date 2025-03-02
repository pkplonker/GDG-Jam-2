﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FogOfWar : MonoBehaviour
{
	public List<GameObject> characters;
	public GameObject scoutObject;

	private SpriteRenderer sr;
	private int width;
	private Texture2D tex;
	private int height;

	[SerializeField]
	private Color revealedColor;

	[SerializeField]
	private Color hiddenColor;

	[SerializeField]
	private int normalRange = 3;

	[SerializeField]
	private int scoutRange = 5;

	[SerializeField]
	private int startSize = 15;

	private MapGenerator mapGenerator;

	private void Awake()
	{
		MapGenerator.OnMapGenerated += OnMapGenerated;
		sr = GetComponent<SpriteRenderer>();
	}

	private void OnDestroy()
	{
		MapGenerator.OnMapGenerated -= OnMapGenerated;
	}

	private void Update()
	{
		if (characters == null || !characters.Any()) return;
		Vector2Int pos = default;
		foreach (var testObject in characters)
		{
			pos = testObject.transform.position.V2Int();
			SetPosition(pos, normalRange);
		}

		pos = scoutObject.transform.position.V2Int();
		SetPosition(pos, scoutRange);
		tex.Apply();
	}

	private void SetPosition(Vector2Int pos, int size)
	{
		var halfSize = Mathf.FloorToInt((float) size / 2f);
		for (var x = -halfSize; x <= halfSize; x++)
		{
			for (var y = -halfSize; y <= halfSize; y++)
			{
				int pixelX = pos.x + x;
				int pixelY = pos.y + y;

				if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < height)
				{
					if (Vector2Int.Distance(new Vector2Int(pixelX, pixelY), pos) > halfSize)
					{
						continue;
					}

					tex.SetPixel(pixelX, pixelY, revealedColor);
				}
			}
		}
	}

	private void OnMapGenerated(MapGenerator obj)
	{
		mapGenerator = obj;

		width = MapGenerator.MapData.GetLength(0);
		height = MapGenerator.MapData.GetLength(1);
		tex = new Texture2D(width, height);
		tex.wrapMode = TextureWrapMode.Clamp;
		Color[] pixels = new Color[width * height];
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = hiddenColor;
		}

		tex.SetPixels(pixels);

		var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);

		sr.sprite = sprite;
		SetPosition(obj.startRoom.bounds.center.V2Int(), startSize);
		tex.Apply();
	}

	private void OnDrawGizmos()
	{
		// if (mapGenerator != null)
		// {
		// 	Gizmos.color = Color.red;
		// 	Gizmos.DrawSphere(mapGenerator.startRoom.bounds.center, 1f);
		// }
	}
}
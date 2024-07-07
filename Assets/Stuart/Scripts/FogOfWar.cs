using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FogOfWar : MonoBehaviour
{
	[SerializeField]
	private GameObject testObject;
	private SpriteRenderer sr;
	private int width;
	private Texture2D tex;
	private int height;
	[SerializeField]
	private Color revealedColor;
	[SerializeField]
	private Color hiddenColor;

	[SerializeField]
	private GameObject scoutObject;
	[SerializeField]
	private int normalRange = 3;
	[SerializeField]
	private int scoutRange = 5;

	private void Awake()
	{
		MapGenerator.OnMapGenerated += OnMapGenerated;
		sr = GetComponent<SpriteRenderer>();
	}

	private void Update()
	{
		Vector2Int pos = testObject.transform.position.V2Int();
		SetPosition(pos,normalRange);
		pos = testObject.transform.position.V2Int();
		SetPosition(pos,scoutRange);

	}

	private void SetPosition(Vector2Int pos, int i)
	{
		tex.SetPixel(pos.x,pos.y,revealedColor);
		tex.Apply();
	}

	private void OnMapGenerated(MapGenerator obj)
	{
		width = MapGenerator.MapData.GetLength(0);
		height = MapGenerator.MapData.GetLength(1);
		tex = new Texture2D(width,height);
		Color[] pixels = new Color[width * height];
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = hiddenColor;
		}

		// Set the pixels of the texture
		tex.SetPixels(pixels);

		// Apply the changes to the texture
		tex.Apply();
		var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);

		sr.sprite = sprite;
	}
}
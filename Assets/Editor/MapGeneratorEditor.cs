using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var mapGenerator = (MapGenerator) target;
		base.OnInspectorGUI();
		if (GUILayout.Button("Generate Points"))
		{
			mapGenerator.Generate();
		}

		if (GUILayout.Button("Clear"))
		{
			mapGenerator.Clear();
		}

		if (GUILayout.Button("Next"))
		{
			Gen(mapGenerator);
		}
	}

	private static void Gen(MapGenerator mapGenerator)
	{
		mapGenerator.MapArgs.seeds.currentSeed++;
		mapGenerator.Generate();
		if (mapGenerator.keyFindRooms.Count <= 2)
		{
			Gen(mapGenerator);
		}
	}
}
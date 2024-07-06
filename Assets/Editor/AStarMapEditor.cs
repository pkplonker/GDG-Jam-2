using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AStarMap))]

public class AStarMapEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var aStarMap = (AStarMap) target;
		base.OnInspectorGUI();
		if (GUILayout.Button("Clear"))
		{
			aStarMap.ClearNodes();
		}
	}
}
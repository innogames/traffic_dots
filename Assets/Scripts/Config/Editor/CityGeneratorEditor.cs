using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	[CustomEditor(typeof(CityGenerator))]
	public class CityGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var cityGen = (CityGenerator) target;
			EditorGUILayout.BeginVertical();
			if (GUILayout.Button("Generate"))
			{
				cityGen.Generate(cityGen.Config);
			}

			if (GUILayout.Button("Clean"))
			{
				cityGen.Clean();
			}

			EditorGUILayout.EndVertical();
		}
	}
}
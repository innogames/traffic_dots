using System.Linq;
using Config.Proxy;
using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	[CustomEditor(typeof(CityConfig))]
	public class CityConfigEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var config = (CityConfig) target;
			if (GUILayout.Button("Populate"))
			{
				config.Segments = Resources.FindObjectsOfTypeAll<RoadSegment>()
					.Where(seg => EditorUtility.IsPersistent(seg.gameObject)).ToArray();
				EditorUtility.SetDirty(config);
			}
		}
	}
}
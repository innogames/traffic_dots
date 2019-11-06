using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	public class SnapEditor<T> : Editor where T:MonoBehaviour
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.BeginVertical();
			var myTarget = (T) target;
			var trans = myTarget.transform;
			
			if (GUILayout.Button("Snap"))
			{
				var pos = trans.position;
				trans.position = new Vector3(Mathf.RoundToInt(pos.x),
					Mathf.RoundToInt(pos.y),
					Mathf.RoundToInt(pos.z));
			}

			OnCustomInspector(myTarget);

			EditorGUILayout.EndVertical();
		}

		protected virtual void OnCustomInspector(T myTarget)
		{
		}
	}
}
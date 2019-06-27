using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Node))]
	public class NodeEditor : Editor
	{
		private static Node _connectFrom;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.BeginVertical();
			var thisNode = (Node) target;
			if (_connectFrom != null && _connectFrom != thisNode)
			{
				var startNode = _connectFrom;
				var endNode = thisNode;
				var root = startNode.transform.root;
				var go = new GameObject("con_" + startNode.gameObject.name + "_" + endNode.gameObject.name);
				go.transform.SetParent(root);
				go.transform.position = (startNode.transform.position + endNode.transform.position) * 0.5f;
				var con = go.AddComponent<Connection>();
				con.StartNode = startNode;
				con.EndNode = endNode;
				_connectFrom = null;
			}

			if (GUILayout.Button("Connect"))
			{
				_connectFrom = thisNode;
			}

			if (GUILayout.Button("Snap"))
			{
				var pos = thisNode.transform.position;
				thisNode.transform.position = new Vector3(Mathf.RoundToInt(pos.x),
					Mathf.RoundToInt(pos.y),
					Mathf.RoundToInt(pos.z));
			}


//			var node = (Node) target;
//			if (GUILayout.Button("Delete"))
//			{
//			}

			EditorGUILayout.EndVertical();
		}
	}
}
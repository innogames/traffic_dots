using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Node))]
	public class NodeEditor : SnapEditor<Node>
	{
		private static Node _connectFrom;

		protected override void OnCustomInspector(Node thisNode)
		{
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
		}
	}
}
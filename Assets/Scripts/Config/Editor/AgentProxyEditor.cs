using System.Linq;
using UnityEditor;

namespace Config
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(AgentProxy))]
	public class AgentProxyEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var agents = targets.OfType<AgentProxy>();
			foreach (var agent in agents)
			{
				
			}
		}
	}
}
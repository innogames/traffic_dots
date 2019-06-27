using System.Linq;
using Config.Proxy;
using UnityEditor;

namespace Config.CityEditor
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
using UnityEngine;

namespace Config
{
	public class CityGenerator : BaseGenerator
	{
		public CityConfig Config;
#if UNITY_EDITOR
		public override void Generate(CityConfig config)
		{
			base.Generate(config);

			//nodes
			var nodes = FindObjectsOfType<Node>();
			foreach (var node in nodes)
			{
				node.Generate(config);
			}

			//connections
			var connections = FindObjectsOfType<Connection>();
			foreach (var connection in connections)
			{
				connection.Generate(config);
			}
		}
#endif
	}
}
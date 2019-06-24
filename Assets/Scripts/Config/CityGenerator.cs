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

		private void Awake()
		{
			PlayModeGenerate(Config);
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);

			//nodes
			var nodes = FindObjectsOfType<Node>();
			foreach (var node in nodes)
			{
				node.PlayModeGenerate(config);
			}

			//connections
			var connections = FindObjectsOfType<Connection>();
			foreach (var connection in connections)
			{
				connection.PlayModeGenerate(config);
			}
		}

		public override void Clean()
		{
			//nodes
			var nodes = FindObjectsOfType<Node>();
			foreach (var node in nodes)
			{
				node.Clean();
			}

			//connections
			var connections = FindObjectsOfType<Connection>();
			foreach (var connection in connections)
			{
				connection.Clean();
			}			
		}
#endif
	}
}
using System.Collections.Generic;
using System.Linq;
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

			foreach (var obj in GetGenerators())
			{
				obj.Generate(config);
			}
		}

		public override void Clean()
		{
			foreach (var obj in GetGenerators())
			{
				obj.Clean();
			}
		}
#endif
		private static IEnumerable<BaseGenerator> GetGenerators()
		{
			//the order here is important!
			return FindObjectsOfType<Node>().Cast<BaseGenerator>()
				.Concat(FindObjectsOfType<Connection>())
				.Concat(FindObjectsOfType<RoadSegment>())
				.Concat(FindObjectsOfType<ConnectionSpawner>())
				.Concat(FindObjectsOfType<AgentSpawner>());
		}

		private void Awake()
		{
			PlayModeGenerate(Config);
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);

			foreach (var obj in GetGenerators())
			{
				obj.PlayModeGenerate(config);
			}
		}
	}
}
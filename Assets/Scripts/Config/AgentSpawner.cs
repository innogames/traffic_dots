using Config.Proxy;
using Model.Components;
using Unity.Entities;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace Config
{
	public class AgentSpawner : BaseGenerator
	{
		public GameObject Agent;
		public Connection SpawnConnection;
		public Connection TargetConnection;
		public int Interval;

#if UNITY_EDITOR
		public GameObjectEntity LinkedSpawnConnection;
		public GameObjectEntity LinkedTargetConnection;

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			LinkedSpawnConnection = SpawnConnection.GetComponent<GameObjectEntity>();
			LinkedTargetConnection = TargetConnection.GetComponent<GameObjectEntity>();

			gameObject.AddComponent<TimerProxy>().Value = new Model.Components.Timer
			{
				Frames = Interval,
				TimerType = TimerType.Ticking,
			};
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			GameObjectEntity goEnt;
			var agentEntity = (goEnt = Agent.GetComponent<GameObjectEntity>()) != null
				? goEnt.Entity
				: GameObjectConversionUtility.ConvertGameObjectHierarchy(Agent, World.Active);
			gameObject.AddComponent<AgentSpawnerProxy>().Value = new Model.Components.AgentSpawner
			{
//				Agent = agentEntity, //TODO this no longer work!
			};
			var spawnLocation = gameObject.AddComponent<ConnectionCoordProxy>();
			spawnLocation.Value = new ConnectionCoord()
			{
				Connection = LinkedSpawnConnection.Entity,
				Coord = 0f, //this value is not used
			};
			var targetLocation = gameObject.AddComponent<ConnectionTargetProxy>();
			targetLocation.Value = new ConnectionTarget()
			{
				Connection = LinkedTargetConnection.Entity,
			};
		}
#endif
	}
}
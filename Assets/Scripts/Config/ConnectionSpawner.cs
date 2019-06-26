using System.Linq;
using Config.Proxy;
using Model.Components;
using Model.Components.Buffer;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	[RequireComponent(typeof(Connection))]
	public class ConnectionSpawner : BaseGenerator
	{
		public GameObject[] Agents;
		[EnumFlags] public TargetType TargetMask;
		public int Interval = 60;

		public TargetMarker[] Targets;

		protected override bool ShouldCleanComponent() => false;

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<TimerProxy>().Value = new Timer
			{
				Frames = Interval,
				TimerType = TimerType.Ticking,
			};
			gameObject.AddComponent<TimerStateProxy>().Value = new TimerState
			{
				CountDown = Interval,
			};
			Targets = FindObjectsOfType<TargetMarker>()
				.Where(marker => (marker.TargetMask & TargetMask) != 0).ToArray();
		}

		private void OnDrawGizmosSelected()
		{
			if (Targets == null || Targets.Length == 0) return;
			var connection = GetComponent<Connection>();
			Gizmos.color = Color.cyan;
			foreach (var target in Targets)
			{
				if (target == null) continue;
				var conTarget = target.GetComponent<Connection>();
				Gizmos.DrawLine(connection.GetMidPoint(), conTarget.GetMidPoint());
			}
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);

			gameObject.AddComponent<AgentSpawnerProxy>().Value = new Model.Components.AgentSpawner
			{
				CurrentIndex = Random.Range(0, Agents.Length),
			};
			gameObject.AddComponent<SpawnerBufferProxy>().SetValue(
				Agents.Select(agent => new SpawnerBuffer
				{
					Agent = GameObjectConversionUtility.ConvertGameObjectHierarchy(agent, World.Active)
				}).ToArray());
			
			var spawnLocation = gameObject.AddComponent<ConnectionCoordProxy>();
			spawnLocation.Value = new ConnectionCoord()
			{
				Connection = gameObject.GetComponent<GameObjectEntity>().Entity, //itself
				Coord = 0f, //this value is not used
			};
			var targetLocation = gameObject.AddComponent<ConnectionTargetProxy>();
			targetLocation.Value = new ConnectionTarget()
			{
				Connection = Entity.Null, //to be filled by TargetSeeker
			};
			gameObject.AddComponent<TargetSeekerProxy>().Value = new TargetSeeker()
			{
				TargetMask = (int)TargetMask,
				LastTargetIndex = 0,
			};
			var targets = Targets
				.Select(marker => new TargetBuffer
				{
					Target = marker.GetComponent<GameObjectEntity>().Entity
				})
				.ToArray();
			gameObject.AddComponent<TargetBufferProxy>().SetValue(targets);
		}
	}
}
using System.Linq;
using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	[RequireComponent(typeof(Connection))]
	public class ConnectionSpawner : BaseGenerator
	{
		public GameObject Agent;
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
				var conTarget = target.GetComponent<Connection>();
				Gizmos.DrawLine(connection.GetMidPoint(), conTarget.GetMidPoint());
			}
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
				Agent = agentEntity,
			};
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
using System.Collections.Generic;
using System.Linq;
using Config.Proxy;
using Model.Components;
using Model.Components.Buffer;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	public class RoadSegment : BaseGenerator
	{
		public CityConfig Config;

		public Connector[] Connectors => GetComponentsInChildren<Connector>();

		public TrafficPhases[] Phases;

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			if (IsIntersection())
			{
				gameObject.AddComponent<GameObjectEntity>();
				gameObject.AddComponent<IntersectionProxy>().Value = new Intersection
				{
					Phase = 0,
					PhaseType = IntersectionPhaseType.Enable,
				};
				int frames = Phases[0].Frames;
				gameObject.AddComponent<TimerProxy>().Value = new Timer
				{
					Frames = frames,
					TimerType = TimerType.Ticking,
				};
				gameObject.AddComponent<TimerStateProxy>().Value = new TimerState
				{
					CountDown = frames,
				};
			}
		}

		private Entity GetConnectionEntity(Connection connection)
		{
			return connection == null ? Entity.Null : connection.GetComponent<GameObjectEntity>().Entity;
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			var list = Phases.Select(phase => new IntersectionPhaseBuffer
			{
				ConnectionA = GetConnectionEntity(phase.ConnectionA),
				ConnectionB = GetConnectionEntity(phase.ConnectionB),
				Frames = phase.Frames,
			}).ToList();
			gameObject.AddComponent<IntersectionPhaseBufferProxy>().SetValue(list);
		}

		public bool IsIntersection()
		{
			return Phases.Length > 0;
		}

		public ConnectionTrafficType GetConnectionTrafficType(Connection connection)
		{
			if (IsIntersection())
			{
				bool isEnable = Phases[0].ConnectionA == connection ||
				                Phases[0].ConnectionB == connection;
				return isEnable ? ConnectionTrafficType.PassThrough : ConnectionTrafficType.NoEntrance;
			}
			else
			{
				return ConnectionTrafficType.Normal;
			}
		}
	}
}
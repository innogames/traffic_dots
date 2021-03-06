using System.Collections.Generic;
using System.Linq;
using Config.Proxy;
using Model.Components;
using Model.Components.Buffer;
using Unity.Entities;

namespace Config
{
	public class RoadSegment : BaseGenerator
	{
		public float SpeedMultiplier = 1f;
		public int Level = 1;
		public TrafficPhases[] Phases;

		public Connector[] Connectors => GetComponentsInChildren<Connector>();


		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			if (IsIntersection())
			{
				var phaseList = new List<IntersectionPhaseBuffer>();
				int index = 0;
				foreach (var phase in Phases)
				{
					phaseList.Add(new IntersectionPhaseBuffer
					{
						StartIndex = index,
						EndIndex = index + phase.Connections.Length - 1,
						Frames = phase.Frames,
					});
					index += phase.Connections.Length;
				}

				gameObject.AddComponent<IntersectionPhaseBufferProxy>().SetValue(phaseList);
				var conList = Phases.SelectMany(phase => phase.Connections).Select(con => new IntersectionConBuffer()
				{
					Connection = con.GetComponent<GameObjectEntity>().Entity,
				}).ToList();
				gameObject.AddComponent<IntersectionConBufferProxy>().SetValue(conList);
			}
		}

		public bool IsIntersection()
		{
			return Phases.Length > 0;
		}

#if UNITY_EDITOR
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

		public ConnectionTrafficType GetConnectionTrafficType(Connection connection)
		{
			if (Phases.SelectMany(phase => phase.Connections).Contains(connection))
			{
				bool isEnable = Phases[0].Connections.Contains(connection);
				return isEnable ? ConnectionTrafficType.PassThrough : ConnectionTrafficType.NoEntrance;
			}
			else
			{
				return ConnectionTrafficType.Normal;
			}
		}
#endif
	}
}
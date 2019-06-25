using Model.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(NetworkCreationSystem))]
	[UpdateBefore(typeof(AgentQueueSystem))]
	public class IntersectionSystem : JobComponentSystem
	{
//		[BurstCompile]
		private struct OperateJob : IJobForEachWithEntity<Intersection, Timer, TimerState>
		{
			[ReadOnly] public BufferFromEntity<IntersectionPhaseBuffer> PhaseBuffer;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLength> ConLengths;
			[ReadOnly] public ComponentDataFromEntity<ConnectionState> ConStates;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<ConnectionTraffic> ConnectionTraffics;

			public void Execute(Entity entity, int index, ref Intersection intersection,
				ref Timer timer, ref TimerState timerState)
			{
				//expect the connection traffic type to be PassThrough from the beginning
				//expect phase type to be Enable
				//timer to be at the value of the first phase
				if (timerState.CountDown == 0) //at the end of a phase
				{
					var phases = PhaseBuffer[entity];
					var phase = phases[intersection.Phase];
					var connectionAEnt = phase.ConnectionA;
					var connectionBEnt = phase.ConnectionB;
					switch (intersection.PhaseType)
					{
						case IntersectionPhaseType.Enable:
							intersection.PhaseType = IntersectionPhaseType.ClearingTraffic;
							ChangeConnectionTraffic(ref connectionAEnt, ConnectionTrafficType.NoEntrance);
							ChangeConnectionTraffic(ref connectionBEnt, ConnectionTrafficType.NoEntrance);
							timer.ChangeToEveryFrame(ref timerState);
							break;
						case IntersectionPhaseType.ClearingTraffic:
							if (CheckConnectionEmpty(ref connectionAEnt)
							&& CheckConnectionEmpty(ref connectionBEnt))
							{
								//move to next phase
								intersection.PhaseType = IntersectionPhaseType.Enable;
								intersection.Phase = (intersection.Phase + 1) % phases.Length;
								var nextPhase = phases[intersection.Phase];
								timerState.CountDown = timer.Frames = nextPhase.Frames;
								timer.TimerType = TimerType.Ticking;
								ChangeConnectionTraffic(ref nextPhase.ConnectionA, ConnectionTrafficType.PassThrough);
								ChangeConnectionTraffic(ref nextPhase.ConnectionB, ConnectionTrafficType.PassThrough);
							}
							break;
					}					
				}
			}

			private bool CheckConnectionEmpty(ref Entity connectionAEnt)
			{
				var conState = ConStates[connectionAEnt];
				var conLen = ConLengths[connectionAEnt];
				return conState.IsEmpty(ref conLen);
			}

			private void ChangeConnectionTraffic(ref Entity connectionAEnt, ConnectionTrafficType trafficType)
			{
				if (connectionAEnt != Entity.Null)
				{
					ConnectionTraffics[connectionAEnt] = new ConnectionTraffic
					{
						TrafficType = trafficType,
					};
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new OperateJob
			{
				PhaseBuffer = GetBufferFromEntity<IntersectionPhaseBuffer>(),
				ConnectionTraffics = GetComponentDataFromEntity<ConnectionTraffic>(),
				ConLengths = GetComponentDataFromEntity<ConnectionLength>(),
				ConStates = GetComponentDataFromEntity<ConnectionState>(),
			}.Schedule(this, inputDeps);
		}
	}
}
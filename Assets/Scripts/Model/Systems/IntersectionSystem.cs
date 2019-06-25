using Model.Components;
using Model.Components.Buffer;
using Model.Systems.States;
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
			[ReadOnly] public BufferFromEntity<IntersectionConBuffer> ConBuffer;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLength> ConLengths;
			[ReadOnly] public ComponentDataFromEntity<ConnectionState> ConStates;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionTraffic> ConnectionTraffics;

			public void Execute(Entity entity, int index, ref Intersection intersection,
				ref Timer timer, ref TimerState timerState)
			{
				//expect the connection traffic type to be PassThrough from the beginning
				//expect phase type to be Enable
				//timer to be at the value of the first phase
				if (timerState.CountDown == 0) //at the end of a phase
				{
					var phases = PhaseBuffer[entity];
					var cons = ConBuffer[entity];
					var phase = phases[intersection.Phase];

					switch (intersection.PhaseType)
					{
						case IntersectionPhaseType.Enable:
							intersection.PhaseType = IntersectionPhaseType.ClearingTraffic;
							for (int i = phase.StartIndex; i <= phase.EndIndex; i++)
							{
								var conEnt = cons[i].Connection;
								ChangeConnectionTraffic(ref conEnt, ConnectionTrafficType.NoEntrance);
							}

							timer.ChangeToEveryFrame(ref timerState);
							break;
						case IntersectionPhaseType.ClearingTraffic:
							bool connectionEmpty = true;
							for (int i = phase.StartIndex; i <= phase.EndIndex; i++)
							{
								var conEnt = cons[i].Connection;
								if (!CheckConnectionEmpty(ref conEnt))
								{
									connectionEmpty = false;
									break;
								}
							}

							if (connectionEmpty)
							{
								//move to next phase
								intersection.PhaseType = IntersectionPhaseType.Enable;
								intersection.Phase = (intersection.Phase + 1) % phases.Length;
								var nextPhase = phases[intersection.Phase];
								timerState.CountDown = timer.Frames = nextPhase.Frames;
								timer.TimerType = TimerType.Ticking;

								for (int i = nextPhase.StartIndex; i <= nextPhase.EndIndex; i++)
								{
									var conEnt = cons[i].Connection;
									ChangeConnectionTraffic(ref conEnt,
										ConnectionTrafficType.PassThrough);
								}
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
				ConBuffer = GetBufferFromEntity<IntersectionConBuffer>(),
				ConnectionTraffics = GetComponentDataFromEntity<ConnectionTraffic>(),
				ConLengths = GetComponentDataFromEntity<ConnectionLength>(),
				ConStates = GetComponentDataFromEntity<ConnectionState>(),
			}.Schedule(this, inputDeps);
		}
	}
}
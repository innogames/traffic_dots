using Model.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(NetworkCreationSystem))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class IntersectionSystem : JobComponentSystem
	{
		[BurstCompile]
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
					var connectionEnt = phase.Connection;
					switch (intersection.PhaseType)
					{
						case IntersectionPhaseType.Enable:
							intersection.PhaseType = IntersectionPhaseType.ClearingTraffic;
							ConnectionTraffics[connectionEnt] = new ConnectionTraffic
							{
								TrafficType = ConnectionTrafficType.NoEntrance,
							};
							timer.ChangeToEveryFrame(ref timerState);
							break;
						case IntersectionPhaseType.ClearingTraffic:
							var conState = ConStates[connectionEnt];
							var conLen = ConLengths[connectionEnt];
							if (conState.IsEmpty(ref conLen))
							{	
								//move to next phase
								intersection.PhaseType = IntersectionPhaseType.Enable;
								intersection.Phase = (intersection.Phase + 1) % phases.Length;
								phase = phases[intersection.Phase];
								timerState.CountDown = timer.Frames = phase.Frames;
								timer.TimerType = TimerType.Ticking;
								ConnectionTraffics[phase.Connection] = new ConnectionTraffic
								{
									TrafficType = ConnectionTrafficType.PassThrough,
								};
							}
							break;
					}

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
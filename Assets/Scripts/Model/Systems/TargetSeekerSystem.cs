using Model.Components;
using Model.Components.Buffer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(AgentSpawningSystem))]
	public class TargetSeekerSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct ScanJob : IJobForEachWithEntity<TargetSeeker, Timer, TimerState, ConnectionTarget>
		{
			[ReadOnly] public BufferFromEntity<TargetBuffer> Targets;

			public void Execute(Entity entity, int index, [ReadOnly] ref TargetSeeker seeker, [ReadOnly] ref Timer timer,
				[ReadOnly] ref TimerState timerState, ref ConnectionTarget connectionTarget)
			{
				if (timerState.CountDown == 0 && timer.TimerType == TimerType.Ticking)
				{
					var targets = Targets[entity];
					connectionTarget.Connection = targets[seeker.LastTargetIndex].Target;
					seeker.LastTargetIndex = (seeker.LastTargetIndex + 1) % targets.Length;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new ScanJob
			{
				Targets = GetBufferFromEntity<TargetBuffer>(),
			}.Schedule(this, inputDeps);
		}
	}
}
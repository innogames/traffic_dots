using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	/// <summary>
	/// TimerState.CountDown will reach 0 in every interval of Timer.Frames frames
	/// if Timer.Frames is set to 1 (the minimum valid value), it means update every frame!
	/// </summary>
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(TimerBufferSystem))]
	public class TimerSystem : JobComponentSystem
	{
		private TimerBufferSystem _bufferSystem;
		[ExcludeComponent(typeof(TimerState))]
		private struct TimerCreationJob : IJobForEachWithEntity<Timer>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public void Execute(Entity entity, int index, ref Timer timer)
			{
				CommandBuffer.AddComponent(index, entity, new TimerState
				{
					CountDown = timer.Frames - 1,
				});
			}
		}

		private struct TimerCountDownJob : IJobForEach<Timer, TimerState>
		{
			public void Execute([ReadOnly] ref Timer timer, ref TimerState state)
			{
				state.CountDown = state.CountDown > 0 ? state.CountDown - 1 : timer.Frames - 1;
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<TimerBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();
			var timerCreation = new TimerCreationJob
			{
				CommandBuffer = commandBuffer,
			}.Schedule(this, inputDeps);
			
			var timerCountdown = new TimerCountDownJob().Schedule(this, timerCreation);
			timerCountdown.Complete();
			return timerCountdown;
		}
	}
}
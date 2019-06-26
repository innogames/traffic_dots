using Model.Components;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	//TODO turn this into batch ComponentSystem!
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class AgentQueuePrepSystem : JobComponentSystem
	{
		private EntityCommandBufferSystem _bufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
		}

		[ExcludeComponent(typeof(ConnectionState))]
		private struct CreationJob : IJobForEachWithEntity<Connection, ConnectionLength>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public void Execute(Entity entity, int index, [ReadOnly] ref Connection connection, 
				[ReadOnly] ref ConnectionLength conLength)
			{
				//TODO change to SetComponent
				CommandBuffer.SetComponent(index, entity, new ConnectionState
				{
					EnterLength = conLength.Length,
				});
				CommandBuffer.SetComponent(index, entity, new ConnectionStateAdjust
				{
					MoveForward = 0f,
					WillRemoveAgent = false,
				});
				//comment out: because added in generator for inspection
				//CommandBuffer.AddBuffer<AgentQueueBuffer>(index, entity);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var creationJob = new CreationJob
			{
				CommandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent(),
			}.Schedule(this, inputDeps);
			creationJob.Complete();
			return creationJob;
		}
	}
}
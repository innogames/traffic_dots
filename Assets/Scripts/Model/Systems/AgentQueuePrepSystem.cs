using Model.Components;
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
		private struct CreationJob : IJobForEachWithEntity<Connection>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public void Execute(Entity entity, int index, [ReadOnly] ref Connection connection)
			{
				CommandBuffer.AddComponent(index, entity, new ConnectionState
				{
					EnterLength = connection.Length,
					ExitLength = 0f,
				});
				CommandBuffer.AddBuffer<AgentQueueBuffer>(index, entity);
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
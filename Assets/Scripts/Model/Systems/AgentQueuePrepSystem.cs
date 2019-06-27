using Model.Components;
using Model.Components.Buffer;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[DisableAutoCreation]
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
				CommandBuffer.AddComponent(index, entity, new ConnectionState
				{
					EnterLength = conLength.Length,
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
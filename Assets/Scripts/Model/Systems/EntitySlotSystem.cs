using Model.Components;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	public struct EntitySlotDone: ISystemStateComponentData{}
	
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class EntitySlotSystem : JobComponentSystem
	{
		private EntityCommandBufferSystem _bufferSystem;
		
		[ExcludeComponent(typeof(EntitySlotDone))]
		private struct CreationJob : IJobForEachWithEntity<EntitySlot>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			
			public void Execute(Entity entity, int index, [ReadOnly] ref EntitySlot slots)
			{
				var writeBuffer = CommandBuffer.AddBuffer<EntitySlotBuffer>(index, entity);
				writeBuffer.Reserve(slots.SlotCount);
				for (int i = 0; i < slots.SlotCount; i++)
				{
					writeBuffer.Add(new EntitySlotBuffer
					{
						Agent = Entity.Null,
					});
				}
				CommandBuffer.AddComponent(index, entity, new EntitySlotDone());
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();
			var creationJob = new CreationJob
			{
				CommandBuffer = commandBuffer,
			}.Schedule(this, inputDeps);
			creationJob.Complete();
			return creationJob;
		}
	}
}
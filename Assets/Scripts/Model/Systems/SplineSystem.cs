using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Model.Systems
{
	public struct SplineDone : ISystemStateComponentData
	{			
	}
	
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class SplineSystem : JobComponentSystem
	{
		private EntityCommandBufferSystem _bufferSystem;
		
		[ExcludeComponent(typeof(SplineDone))]
		private struct SplineSegmentCreation : IJobForEachWithEntity<Spline, EntitySlot>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public void Execute(Entity entity, int index, [ReadOnly] ref Spline spline, [ReadOnly] ref EntitySlot slots)
			{
				var buffer = CommandBuffer.AddBuffer<SplineSegmentBuffer>(index, entity);
				buffer.Reserve(slots.SlotCount);
				//TODO compute correct spline length here!
				float totalLength = math.length(spline.a - spline.d);
				float segmentLength = totalLength / slots.SlotCount;
				for (int i = 0; i < slots.SlotCount; i++)
				{
					buffer.Add(new SplineSegmentBuffer
					{
						Length = (int)math.ceil(segmentLength),
					});
				}
				CommandBuffer.AddComponent(index, entity, new SplineDone());
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
			var creationJob = new SplineSegmentCreation
			{
				CommandBuffer = commandBuffer,
			}.Schedule(this, inputDeps);
			creationJob.Complete();
			return creationJob;
		}
	}
}
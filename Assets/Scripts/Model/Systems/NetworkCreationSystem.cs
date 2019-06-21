using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems.City
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(CityAddConnectionSeqSystem))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class NetworkCreationSystem : JobComponentSystem
	{
		private PathCacheCommandBufferSystem _endFrameBarrier;
		
		[ExcludeComponent(typeof(NetworkSharedDataNew))]
		private struct CacheCompute : IJobForEachWithEntity<Network>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[Unity.Collections.ReadOnly] public BufferFromEntity<NetAdjust> NetAdjusts;

			public void Execute(Entity entity, int index, [Unity.Collections.ReadOnly] ref Network network)
			{
				var networkShared = NetworkSharedDataNew.Create(entity);
				var adjusts = NetAdjusts[entity];
				for (int i = 0; i < adjusts.Length; i++)
				{
					var adjust = adjusts[i];
					networkShared.AddConnection(adjust.StartNode, adjust.EndNode, adjust.Cost, adjust.Connection);
				}

				networkShared.Compute(index, CommandBuffer);
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			_endFrameBarrier = World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _endFrameBarrier.CreateCommandBuffer().ToConcurrent();
			var cacheCompute = new CacheCompute
			{
				CommandBuffer = commandBuffer,
				NetAdjusts = GetBufferFromEntity<NetAdjust>(),
			}.Schedule(this, inputDeps);

			return cacheCompute;
		}
	}
}
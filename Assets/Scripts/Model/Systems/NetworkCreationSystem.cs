using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(CityAddConnectionSeqSystem))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class NetworkCreationSystem : JobComponentSystem
	{
		private PathCacheCommandBufferSystem _endFrameBarrier;
		
		private struct CacheCompute : IJobForEachWithEntity<Network>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[Unity.Collections.ReadOnly] public BufferFromEntity<NetAdjust> NetAdjusts;

			public void Execute(Entity entity, int index, [Unity.Collections.ReadOnly] ref Network network)
			{
				var networkCache = NetworkCache.Create(entity);
				var adjusts = NetAdjusts[entity];
				for (int i = 0; i < adjusts.Length; i++)
				{
					var adjust = adjusts[i];
					networkCache.AddConnection(adjust.StartNode, adjust.EndNode, adjust.Cost, adjust.Connection);
				}

				networkCache.Compute(index, CommandBuffer);
				networkCache.Dispose();
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
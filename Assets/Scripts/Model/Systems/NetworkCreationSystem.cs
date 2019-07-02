using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(NetworkGroupCreationSystem))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class NetworkCreationSystem : JobComponentSystem
	{
		private PathCacheCommandBufferSystem _endFrameBarrier;
		
		[ExcludeComponent(typeof(NetworkDone))]
		private struct CacheCompute : IJobForEachWithEntity<Network>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public BufferFromEntity<NetAdjust> NetAdjusts;

			public void Execute(Entity entity, int index, [ReadOnly] ref Network network)
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
				CommandBuffer.AddComponent(index, entity, new NetworkDone());
				CommandBuffer.RemoveComponent<NetAdjust>(index, entity);
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

			cacheCompute.Complete();
			return cacheCompute;
		}
	}
}
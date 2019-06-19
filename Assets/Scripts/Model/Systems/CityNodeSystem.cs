using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems.City
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(NodeDataCommandBufferSystem))]
	public class CityNodeSystem : JobComponentSystem
	{
		private NodeDataCommandBufferSystem _endFrameBarrier;
		private EntityArchetype _networkArchetype;

//		[BurstCompile]
		[ExcludeComponent(typeof(NodeData))]
		private struct AddNodeJob : IJobForEachWithEntity<Node>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public EntityArchetype NetworkEntity;
			public void Execute(Entity entity, int index, [ReadOnly] ref Node node)
			{
				var network = CommandBuffer.CreateEntity(index, NetworkEntity);
				CommandBuffer.SetComponent(index, network, new NetCount {Count = 1});
				CommandBuffer.AddComponent(index, entity, new NodeData
				{
					ClosestExit = Entity.Null,
					Network = network,
				});
			}
		}
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			_endFrameBarrier = World.GetOrCreateSystem<NodeDataCommandBufferSystem>();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(NetworkSharedData)),
				new ComponentType(typeof(NetCount)));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _endFrameBarrier.CreateCommandBuffer().ToConcurrent();
			var addNodeJob = new AddNodeJob
			{
				CommandBuffer = commandBuffer,
				NetworkEntity = _networkArchetype,
			}.Schedule(this, inputDeps);

			return addNodeJob;
		}
	}
}
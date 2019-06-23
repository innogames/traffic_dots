using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(NetworkCreationSystem))]
	public class PathSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem _endFrameBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			_endFrameBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _endFrameBarrier.CreateCommandBuffer().ToConcurrent();
			var pathCompute = new PathCompute
			{
				CommandBuffer = commandBuffer,
				Next = GetBufferFromEntity<NextBuffer>(),
				Connections = GetComponentDataFromEntity<Connection>(),
				Indexes = GetComponentDataFromEntity<IndexInNetwork>()
			}.Schedule(this, inputDeps);

			pathCompute.Complete();
			return pathCompute;
		}

		[RequireComponentTag(typeof(Agent))]
		private struct PathCompute : IJobForEachWithEntity<ConnectionLocation, ConnectionDestination>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			public void Execute(Entity entity, int index,
				[ReadOnly] ref ConnectionLocation location,
				[ReadOnly] ref ConnectionDestination destination)
			{
				var startNode = Connections[location.Connection].EndNode; //assume that it is one way road!
				var endNode = Connections[destination.Connection].EndNode;
				if (startNode == endNode)
				{
					//TODO remove agent here!
					CommandBuffer.RemoveComponent<ConnectionDestination>(index, entity);
				}
				else
				{
					//TODO handle different network here
					var next = Next[startNode][Indexes[endNode].Index].Connection;
					location.Connection = next;
					CommandBuffer.SetComponent(index, entity, location);
				}
			}
		}
	}
}
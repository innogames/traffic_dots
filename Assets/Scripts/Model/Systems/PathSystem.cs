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

		private struct PathCompute : IJobForEachWithEntity<Agent, PathIntent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			public void Execute(Entity entity, int index, [ReadOnly] ref Agent agent,
				[ReadOnly] ref PathIntent pathIntent)
			{
				var startNode = Connections[agent.Connection].EndNode; //assume that it is one way road!
				var endNode = pathIntent.EndNode;
				if (startNode == endNode)
				{
					CommandBuffer.RemoveComponent<PathIntent>(index, entity);
				}
				else
				{
					//TODO handle different network here
					var next = Next[startNode][Indexes[endNode].Index].Connection;
					agent.Connection = next;
					CommandBuffer.SetComponent(index, entity, agent);
				}
			}
		}
	}
}
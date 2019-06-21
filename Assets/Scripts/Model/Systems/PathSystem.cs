using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems.City
{
	public struct Path : IEquatable<Path>
	{
		public Entity From;
		public Entity To;

		public Path(Entity from, Entity to)
		{
			From = from;
			To = to;
		}

		public bool Equals(Path other)
		{
			return From.Equals(other.From) && To.Equals(other.To);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Path other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (From.GetHashCode() * 397) ^ To.GetHashCode();
			}
		}
	}

	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(CityAddConnectionSeqSystem))]
	[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
	public class PathSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem _endFrameBarrier;
		
		private struct PathCompute : IJobForEachWithEntity<Agent, PathIntent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			public void Execute(Entity entity, int index, [ReadOnly] ref Agent agent, [ReadOnly] ref PathIntent pathIntent)
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
				Indexes = GetComponentDataFromEntity<IndexInNetwork>(),
			}.Schedule(this, inputDeps);

			return pathCompute;
		}
	}
}
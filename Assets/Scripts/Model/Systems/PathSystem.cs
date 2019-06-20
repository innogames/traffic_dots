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
		
		[ExcludeComponent(typeof(PathIntentData))]
		private struct PathCompute : IJobForEachWithEntity<PathIntent, NodeAttachment>
		{
			[ReadOnly] public NativeHashMap<Path, Entity> Next;
			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity entity, int index, ref PathIntent pathIntent, ref NodeAttachment nodeAttachment)
			{
				CommandBuffer.AddComponent(index, entity, new PathIntentData
				{
					CurrentConnection = Next[new Path
					{
						From = nodeAttachment.Node,
						To = pathIntent.EndNode,
					}],
					Lerp = 0f,
				});
			}
		}

		[ExcludeComponent(typeof(NetworkSharedDataNew))]
		private struct CacheCompute : IJobForEachWithEntity<Network>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public BufferFromEntity<NetAdjust> NetAdjusts;

			public void Execute(Entity entity, int index, [ReadOnly] ref Network network)
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
			_endFrameBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _endFrameBarrier.CreateCommandBuffer().ToConcurrent();
			return new CacheCompute
			{
				CommandBuffer = commandBuffer,
				NetAdjusts = GetBufferFromEntity<NetAdjust>(),
			}.Schedule(this, inputDeps);
		}
	}
}
using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(PathCacheCommandBufferSystem))]
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
			var segmentBuffer = GetBufferFromEntity<SplineSegmentBuffer>();

			var moveToNext = new MoveToNextSlot
			{
				SegmentBuffer = segmentBuffer,
				SlotBuffer = GetBufferFromEntity<EntitySlotBuffer>(),
			}.Schedule(this, inputDeps);
			
			var pathCompute = new PathCompute
			{
				CommandBuffer = commandBuffer,
				Next = GetBufferFromEntity<NextBuffer>(),
				Connections = GetComponentDataFromEntity<Connection>(),
				Indexes = GetComponentDataFromEntity<IndexInNetwork>(),
				EntitySlots = GetComponentDataFromEntity<EntitySlot>(),
				SegmentBuffer = segmentBuffer,
			}.Schedule(this, moveToNext);

			pathCompute.Complete();
			return pathCompute;
		}

		[RequireComponentTag(typeof(Agent))]
		private struct PathCompute : IJobForEachWithEntity<ConnectionLocation, ConnectionDestination, Timer, TimerState>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<EntitySlot> EntitySlots;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public BufferFromEntity<SplineSegmentBuffer> SegmentBuffer;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			public void Execute(Entity entity, int index,
				ref ConnectionLocation location,
				[ReadOnly] ref ConnectionDestination destination,
				ref Timer timer, ref TimerState timerState)
			{
				if (timerState.CountDown == 0)
				{
					if (location.Connection == destination.Connection && location.Slot == destination.Slot)
					{
						CommandBuffer.RemoveComponent<ConnectionDestination>(index, entity);						
					}
					else
					{
						if (location.Slot == EntitySlots[location.Connection].SlotCount - 1)
						{
							var startNode = Connections[location.Connection].EndNode;
							var endNode = Connections[destination.Connection].StartNode;
							var next = startNode == endNode
								? destination.Connection
								: Next[startNode][Indexes[endNode].Index].Connection;
							location.Connection = next;
							location.Slot = 0;
							
							timer.Frames = SegmentBuffer[next][0].Length;
							timerState.CountDown = timer.Frames;
						}
					}
				}
			}
		}

		[RequireComponentTag(typeof(Agent))]
		private struct MoveToNextSlot : IJobForEachWithEntity<ConnectionLocation, Timer, TimerState>
		{
			[ReadOnly] public BufferFromEntity<EntitySlotBuffer> SlotBuffer;			
			[ReadOnly] public BufferFromEntity<SplineSegmentBuffer> SegmentBuffer;
			
			public void Execute(Entity entity, int index, ref ConnectionLocation location, 
				ref Timer timer, ref TimerState timerState)
			{
				if (timerState.CountDown == 0)
				{
					var slots = SlotBuffer[location.Connection];
					if (location.Slot < slots.Length - 1 && slots[location.Slot + 1].Agent == Entity.Null)
					{
						slots[location.Slot] = new EntitySlotBuffer {Agent = Entity.Null};
						slots[location.Slot + 1] = new EntitySlotBuffer {Agent = entity};
						location.Slot = location.Slot + 1;
						
						timer.Frames = SegmentBuffer[location.Connection][location.Slot].Length;
						timerState.CountDown = timer.Frames;
					}
				}
			}
		}
	}
}
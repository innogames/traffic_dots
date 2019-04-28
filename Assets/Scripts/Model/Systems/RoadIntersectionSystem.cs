using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	public class RoadIntersectionSystem : JobComponentSystem
	{
		private EntityCommandBufferSystem _barrierSystem;
		private EntityArchetype _intersectionArchetype;

		//check if road exist: hash<xy_vertical>
		//check if intersection exist hash<xy>
		//if intersection already exist --> it's a connection, remove it!
		//if intersection doesn't exist --> check if there is a road crossing that point --> 3 way split!
		//new segment get road id from old one
		
		//intersection will have traffic light component!
		
		//TODO remove road segment
		
		protected override void OnCreate()
		{
			base.OnCreate();
			_barrierSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			_intersectionArchetype = EntityManager.CreateArchetype(typeof(Intersection));
		}

		[ExcludeComponent(typeof(RoadSegmentState))]
		private struct AddIntersectionJob : IJobForEachWithEntity<RoadSegment>
		{
			[ReadOnly] public EntityCommandBuffer CommandBuffer;
			[ReadOnly] public EntityArchetype IntersectionArchetype;
			public void Execute(Entity entity, int index, [ReadOnly] ref RoadSegment roadSegment)
			{
				//create two intersections
				//TODO check if they do not exist
				var intersectA = CommandBuffer.CreateEntity(IntersectionArchetype);
				CommandBuffer.SetComponent(intersectA, new Intersection
				{
					X = roadSegment.X,
					Y = roadSegment.Y,
				});
				var intersectB = CommandBuffer.CreateEntity(IntersectionArchetype);
				CommandBuffer.SetComponent(intersectB, new Intersection
				{
					X = roadSegment.X + (roadSegment.IsVertical ? 0 : 1),
					Y = roadSegment.Y + (roadSegment.IsVertical ? 1 : 0),
				});
				CommandBuffer.AddComponent(entity, new RoadSegmentState
				{
					IntersectionStart = intersectA,
					IntersectionEnd = intersectB,
				});
			}
		}
		
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//detect added road segment
			//segment lengthen road: move intersection
			//cross-road: add intersection
			//compute path map of all intersections
			
			//path finding: move from segment A to segment B
			//each segment is linked to two intersections
			//get the shortest distance of the combination
			//get the list of intersection to cross
			var job = new AddIntersectionJob()
			{
				CommandBuffer = _barrierSystem.CreateCommandBuffer(),
				IntersectionArchetype = _intersectionArchetype,
			}.Schedule(this, inputDeps);
			
			_barrierSystem.AddJobHandleForProducer(job);
			return job;
		}
		
		public void AddRoadSegment(EntityCommandBuffer commandBuffer, int x, int y)
		{
			var entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent(entity, new RoadSegment()
			{
				LaneCount = 1,
				X = x,
				Y = y,
			});
		}
	}
}
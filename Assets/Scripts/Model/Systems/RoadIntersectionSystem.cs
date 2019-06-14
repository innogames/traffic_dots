using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	public class RoadIntersectionSystem : JobComponentSystem
	{
		private const int Size = 50;
		private EntityCommandBufferSystem _barrierSystem;
		private EntityArchetype _intersectionArchetype;
		private NativeHashMap<int, Entity> _intersections;

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
			_intersections = new NativeHashMap<int, Entity>(Size * Size, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_intersections.Dispose();
		}

		private static int CoordinateKey(int x, int y)
		{
			return x * Size + y;
		}

		[ExcludeComponent(typeof(RoadSegmentState))]
		private struct PreAddIntersectionJob : IJobForEachWithEntity<RoadSegment>
		{
			[ReadOnly] public NativeHashMap<int, Entity> Intersections;
			public NativeHashMap<int, Entity>.Concurrent ToBeModified;
			public void Execute(Entity entity, int index, [ReadOnly] ref RoadSegment roadSegment)
			{
				
				
				int keyA = CoordinateKey(roadSegment.X, roadSegment.Y);
				ToBeModified.TryAdd(keyA, Intersections.TryGetValue(keyA, out var outputA) ? outputA : Entity.Null);
				
				int bX = roadSegment.X + (roadSegment.IsVertical ? 0 : 1);
				int bY = roadSegment.Y + (roadSegment.IsVertical ? 1 : 0);
				int keyB = CoordinateKey(bX, bY);
				ToBeModified.TryAdd(keyB, Intersections.TryGetValue(keyB, out var outputB) ? outputB : Entity.Null);
			}
		}
		
		[ExcludeComponent(typeof(RoadSegmentState))]
		private struct AddIntersectionJob : IJobForEachWithEntity<RoadSegment>
		{
			[ReadOnly] public EntityCommandBuffer CommandBuffer;
			[ReadOnly] public EntityArchetype IntersectionArchetype;
			public NativeHashMap<int, Entity>.Concurrent Intersections;
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
				Intersections.TryAdd(CoordinateKey(roadSegment.X, roadSegment.Y), intersectA);
				
				var intersectB = CommandBuffer.CreateEntity(IntersectionArchetype);
				int bX = roadSegment.X + (roadSegment.IsVertical ? 0 : 1);
				int bY = roadSegment.Y + (roadSegment.IsVertical ? 1 : 0);
				CommandBuffer.SetComponent(intersectB, new Intersection
				{
					X = bX,
					Y = bY,
				});
				Intersections.TryAdd(CoordinateKey(bX, bY), intersectB);
				
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
				Intersections = _intersections.ToConcurrent(),
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
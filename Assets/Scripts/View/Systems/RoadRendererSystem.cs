using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using View.Components;

namespace View.Systems
{
	public class RoadRendererSystem : ComponentSystem
	{
		private EntityQuery _segmentGroup;

		protected override void OnCreate()
		{
			base.OnCreate();
			_segmentGroup = EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RoadSegment)},
				None = new ComponentType[] {typeof(RenderMesh)},
			});
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((RoadRenderer renderer) =>
			{
				var entities = _segmentGroup.ToEntityArray(Allocator.TempJob);
				var segments = _segmentGroup.ToComponentDataArray<RoadSegment>(Allocator.TempJob);
				for (int i = 0; i < entities.Length; i++)
				{
					var entity = entities[i];
					PostUpdateCommands.AddSharedComponent(entity, renderer.RoadSegment);
					var segment = segments[i];
					PostUpdateCommands.AddComponent(entity, new Translation()
					{
						Value = new float3(segment.X, 0, segment.Y),
					});
					PostUpdateCommands.AddComponent(entity, new Rotation()
					{
						Value = quaternion.RotateY(segment.IsVertical ? 90 : 0),
					});
					PostUpdateCommands.AddComponent(entity, new LocalToWorld());
				}
				entities.Dispose();
				segments.Dispose();
			});
		}
	}
}
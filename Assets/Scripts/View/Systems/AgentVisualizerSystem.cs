using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using TimerState = Model.Components.TimerState;

namespace View.Systems
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class AgentVisualizerSystem : JobComponentSystem
	{
		[RequireComponentTag(typeof(Agent))]
		private struct MoveJob : IJobForEachWithEntity<ConnectionCoord, Translation, Rotation, Timer, TimerState>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<Spline> Splines;

			public void Execute(Entity entity, int index, [ReadOnly] ref ConnectionCoord coord,
				ref Translation translation, ref Rotation rotation, [ReadOnly] ref Timer timer, [ReadOnly] ref TimerState timerState)
			{
				var spline = Splines[coord.Connection];
				float length = Connections[coord.Connection].Length;
				
				//TODO cache this value every time Coord change!
//				var targetPos = math.lerp(spline.d, spline.a, coord.Coord / length);
//				translation.Value = math.lerp(targetPos, spline.a, (float) timerState.CountDown / timer.Frames);
//				rotation.Value = quaternion.LookRotation(spline.d - spline.a, new float3(0, 1, 0));

				float targetT = 1f - coord.Coord / length;
				float actualT = targetT * (1f- (float)timerState.CountDown / timer.Frames);
				translation.Value = spline.Point(actualT);
				rotation.Value = quaternion.LookRotation(spline.Tangent(actualT), new float3(0, 1, 0));
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new MoveJob
			{
				Splines = GetComponentDataFromEntity<Spline>(),
				Connections = GetComponentDataFromEntity<Connection>(),
			}.Schedule(this, inputDeps);
		}
	}
}
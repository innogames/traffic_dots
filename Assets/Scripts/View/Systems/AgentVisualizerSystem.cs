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
//		[RequireComponentTag(typeof(Agent))]
		private struct MoveJob : IJobForEach<ConnectionCoord, TailCoord, Translation, Rotation, Timer, TimerState>
		{
			[ReadOnly] public ComponentDataFromEntity<ConnectionLength> ConLengths;
			[ReadOnly] public ComponentDataFromEntity<Spline> Splines;

			public void Execute([ReadOnly] ref ConnectionCoord coord, [ReadOnly] ref TailCoord tailCoord,
				ref Translation translation, ref Rotation rotation, [ReadOnly] ref Timer timer,
				[ReadOnly] ref TimerState timerState)
			{
				float length = ConLengths[coord.Connection].Length;

				var	headPos = ComputePos(ref coord.Connection, ref coord.Coord, ref timer, ref timerState, ref length);
				var	tailPos = ComputePos(ref tailCoord.Connection, ref tailCoord.Coord, ref timer, ref timerState, ref length);

				var tangent = headPos - tailPos;
				translation.Value = (headPos + tailPos) * 0.5f;
				rotation.Value = quaternion.LookRotation(tangent, new float3(0, 1, 0));
			}

			private float3 ComputePos([ReadOnly] ref Entity con, ref float coord, [ReadOnly] ref Timer timer,
				[ReadOnly] ref TimerState timerState, [ReadOnly] ref float length)
			{
				var spline = Splines[con];
				float targetT = 1f - coord / length;
				float actualT = targetT * (1f - (float) timerState.CountDown / timer.Frames);
				return spline.Point(actualT);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new MoveJob
			{
				Splines = GetComponentDataFromEntity<Spline>(),
				ConLengths = GetComponentDataFromEntity<ConnectionLength>(),
			}.Schedule(this, inputDeps);
		}
	}
}
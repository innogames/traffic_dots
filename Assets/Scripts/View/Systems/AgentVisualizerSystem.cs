using Model.Components;
using Unity.Burst;
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
		[BurstCompile]
		[RequireComponentTag(typeof(Agent))]
		private struct MoveJob : IJobForEach<AgentCordInt, AgentStateInt, Translation, Rotation>
		{
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLengths;
			[ReadOnly] public ComponentDataFromEntity<Spline> Splines;

			public void Execute([ReadOnly] ref AgentCordInt head, 
				[ReadOnly] ref AgentStateInt tail,
				ref Translation translation, ref Rotation rotation)
			{
				float3 headPos;
				float3 tailPos;
				{
					var conEnt = head.HeadCon;
					var spline = Splines[head.HeadCon];
					int length = ConLengths[conEnt].Length;
					float actualT = (float) head.HeadCord / length;
					headPos = spline.Point(actualT);
				}
				{
					var conEnt = tail.TailCon;
					var spline = Splines[tail.TailCon];
					int length = ConLengths[conEnt].Length;
					float actualT = (float) tail.TailCord / length;
					tailPos = spline.Point(actualT);
				}

				translation.Value = (headPos + tailPos) * 0.5f;
				rotation.Value = quaternion.LookRotation(tailPos - headPos, math.up());
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new MoveJob
			{
				Splines = GetComponentDataFromEntity<Spline>(),
				ConLengths = GetComponentDataFromEntity<ConnectionLengthInt>(),
			}.Schedule(this, inputDeps);
		}
	}
}
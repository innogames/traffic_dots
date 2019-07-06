using System;
using Model.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Model.Systems
{
	[DisableAutoCreation]
	public class DemoSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct VehicleJob : IJobForEachWithEntity<Agent, AgentStateInt>
		{
			[ReadOnly] 
			public ComponentDataFromEntity<ConnectionLength> RoadLengths;

			[NativeDisableParallelForRestriction] 
			public ComponentDataFromEntity<ConnectionState> RoadStates;

			public void Execute(Entity entity, int index, [ReadOnly] ref Agent agent, 
				ref AgentStateInt state)
			{
				//read from "agent" & "RoadLengths"
				//read & write to "state"
				//read & write to "RoadStates" -> be CAREFUL!
			}
		}
			
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			throw new Exception();
		}
	}
}
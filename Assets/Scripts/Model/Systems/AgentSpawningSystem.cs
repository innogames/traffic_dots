using Model.Components;
using Model.Components.Buffer;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(PathCacheCommandBufferSystem))]
	public class AgentSpawningSystem : JobComponentSystem //TODO consider turning to job!
	{
		private struct SpawnJob : IJobForEachWithEntity_EBCCCCC<SpawnerBuffer, AgentSpawner, Timer, TimerState,
			ConnectionStateInt, ConnectionTarget>
		{
			public EntityCommandBuffer.Concurrent UpdateCommands;

			[ReadOnly] public ComponentDataFromEntity<AgentInt> Agents;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;

			public void Execute(Entity entity, int index,
				DynamicBuffer<SpawnerBuffer> buffer,
				[ReadOnly] ref AgentSpawner spawner,
				ref Timer timer, ref TimerState timerState,
				ref ConnectionStateInt connectionState,
				[ReadOnly] ref ConnectionTarget agentTarget)
			{
				if (timerState.CountDown == 0)
				{
					var agentPrefab = buffer[spawner.CurrentIndex].Agent;
					spawner.CurrentIndex = (spawner.CurrentIndex + 1) % buffer.Length;
					int agentLen = Agents[agentPrefab].Length;
					var connectionTraffic = ConTraffics[entity];

					if (connectionTraffic.TrafficType != ConnectionTrafficType.NoEntrance
					    && connectionState.EnterLength >= agentLen)
					{
						var agentEnt = UpdateCommands.Instantiate(index, agentPrefab);
						UpdateCommands.SetComponent(index, agentEnt, new AgentCordInt
						{
							HeadCon = entity,
							HeadCord = agentLen,
						});
						UpdateCommands.SetComponent(index, agentEnt, new AgentStateInt
						{
							TailCon = entity,
							TailCord = 0,
							MoveDist = connectionState.EnterLength - agentLen,
						});
						UpdateCommands.SetComponent(index, agentEnt, agentTarget);

						connectionState.EnterLength -= agentLen;
						//no pulling needed

						timer.TimerType = TimerType.Ticking;
					}
					else
					{
						timer.ChangeToEveryFrame(ref timerState);
					}
				}
			}
		}

		private EntityCommandBufferSystem _bufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();
			var spawnJob = new SpawnJob
			{
				UpdateCommands = commandBuffer,
				Agents = GetComponentDataFromEntity<AgentInt>(),
				ConTraffics = GetComponentDataFromEntity<ConnectionTraffic>(),
			}.Schedule(this, inputDeps);
			spawnJob.Complete();
			return spawnJob;
		}
	}
}
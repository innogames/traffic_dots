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
	[UpdateBefore(typeof(AgentTrailingSystem))]
	public class AgentSpawningSystem : JobComponentSystem
	{
		private struct SpawnJob : IJobForEachWithEntity_EBCCCCC<SpawnerBuffer, AgentSpawner, Timer, TimerState,
			ConnectionStateInt, ConnectionTarget>
		{
			public EntityCommandBuffer.Concurrent UpdateCommands;

			[ReadOnly] public ComponentDataFromEntity<AgentInt> Agents;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
#if CITY_DEBUG
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
#endif
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
					    && connectionState.EnterLen >= agentLen)
					{
						var agentEnt = UpdateCommands.Instantiate(index, agentPrefab);
						UpdateCommands.SetComponent(index, agentEnt, new AgentCordInt
						{
							HeadCon = entity,
							HeadCord = 0,
						});
						UpdateCommands.SetComponent(index, agentEnt, new AgentStateInt
						{
							TailCon = entity,
							TailCord = -agentLen,
							MoveDist = connectionState.EnterLen,
							MoveForce = 0,
						});
#if CITY_DEBUG
						if (connectionState.EnterLen > ConLens[entity].Length)
						{
							int abc = 123;
						}
#endif
						UpdateCommands.SetComponent(index, agentEnt, agentTarget);

						connectionState.EnterLen -= agentLen;

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
#if CITY_DEBUG
				ConLens = GetComponentDataFromEntity<ConnectionLengthInt>(),
#endif
			}.Schedule(this, inputDeps);
			spawnJob.Complete();
			return spawnJob;
		}
	}
}
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
			ConnectionState, ConnectionTarget>
		{
			public EntityCommandBuffer.Concurrent UpdateCommands;

			[NativeDisableParallelForRestriction] public BufferFromEntity<AgentQueueBuffer> Queues;

			[ReadOnly] public ComponentDataFromEntity<Agent> Agents;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLength> ConLengths;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;

			public void Execute(Entity entity, int index,
				DynamicBuffer<SpawnerBuffer> buffer,
				[ReadOnly] ref AgentSpawner spawner,
				ref Timer timer, ref TimerState timerState,
				ref ConnectionState connectionState,
				[ReadOnly] ref ConnectionTarget agentTarget)
			{
				if (timerState.CountDown == 0)
				{
					var agentPrefab = buffer[spawner.CurrentIndex].Agent;
					spawner.CurrentIndex = (spawner.CurrentIndex + 1) % buffer.Length;
					var agent = Agents[agentPrefab];
					var conSpeed = Connections[entity];
					var conLength = ConLengths[entity];
					var connectionTraffic = ConTraffics[entity];

					if (connectionTraffic.TrafficType != ConnectionTrafficType.NoEntrance
					    && connectionState.CouldAgentEnter(ref agent, ref conLength))
					{
						var agentEnt = UpdateCommands.Instantiate(index, agentPrefab);
						UpdateCommands.SetComponent(index, agentEnt, new ConnectionCoord
						{
							Connection = entity,
							Coord = connectionState.NewAgentCoord(ref conLength),
						});
						UpdateCommands.SetComponent(index, agentEnt, agentTarget);
						int interval = connectionState.FramesToEnter(ref conSpeed);
						UpdateCommands.SetComponent(index, agentEnt, new Timer
						{
							Frames = interval,
							TimerType = TimerType.Ticking,
						});
						UpdateCommands.SetComponent(index, agentEnt,
							new TimerState //need this so that it can move immediately!
							{
								CountDown = interval,
							});

						connectionState.AcceptAgent(ref agent);

						var queue = Queues[entity];
						queue.Add(new AgentQueueBuffer {Agent = agentEnt});

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
				Agents = GetComponentDataFromEntity<Agent>(),
				ConLengths = GetComponentDataFromEntity<ConnectionLength>(),
				Connections = GetComponentDataFromEntity<Connection>(),
				ConTraffics = GetComponentDataFromEntity<ConnectionTraffic>(),
				Queues = GetBufferFromEntity<AgentQueueBuffer>(),
			}.Schedule(this, inputDeps);
			spawnJob.Complete();
			return spawnJob;
		}
	}
}
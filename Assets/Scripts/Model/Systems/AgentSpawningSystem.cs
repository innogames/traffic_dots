using Model.Components;
using Model.Components.Buffer;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(PathCacheCommandBufferSystem))]
	public class AgentSpawningSystem : ComponentSystem //TODO consider turning to job!
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref AgentSpawner spawner, ref Timer timer, ref TimerState timerState,
				ref ConnectionCoord spawnTarget,
				ref ConnectionTarget agentTarget) =>
			{
				if (timerState.CountDown == 0)
				{
					var buffer = EntityManager.GetBuffer<SpawnerBuffer>(entity);
					var agentPrefab = buffer[spawner.CurrentIndex].Agent;
					spawner.CurrentIndex = (spawner.CurrentIndex + 1) % buffer.Length;
					var agent = EntityManager.GetComponentData<Agent>(agentPrefab);
					var targetConnectionEnt = spawnTarget.Connection;
					var conSpeed = EntityManager.GetComponentData<Connection>(targetConnectionEnt);
					var conLength = EntityManager.GetComponentData<ConnectionLength>(targetConnectionEnt);
					var connectionState = EntityManager.GetComponentData<ConnectionState>(targetConnectionEnt);
					var connectionTraffic = EntityManager.GetComponentData<ConnectionTraffic>(targetConnectionEnt);

					if (connectionTraffic.TrafficType != ConnectionTrafficType.NoEntrance
					    && connectionState.CouldAgentFullyEnter(ref agent, ref conLength))
					{
						var agentEnt = PostUpdateCommands.Instantiate(agentPrefab);
						PostUpdateCommands.SetComponent(agentEnt, new ConnectionCoord
						{
							Connection = targetConnectionEnt,
							Coord = connectionState.NewAgentCoord(ref conLength),
						});
						PostUpdateCommands.SetComponent(agentEnt, agentTarget);
						int interval = connectionState.FramesToEnter(ref conSpeed);
						PostUpdateCommands.SetComponent(agentEnt, new Timer
						{
							Frames = interval,
							TimerType = TimerType.Ticking,
						});
						PostUpdateCommands.SetComponent(agentEnt,
							new TimerState //need this so that it can move immediately!
							{
								CountDown = interval,
							});
						PostUpdateCommands.SetComponent(agentEnt,
							new TailCoord
							{
								Connection = targetConnectionEnt,
								Coord = connectionState.NewAgentCoord(ref conLength) + agent.Length,
							});

						connectionState.AcceptAgentFully(ref agent);
						EntityManager.SetComponentData(targetConnectionEnt, connectionState);

						var oldQueue = EntityManager.GetBuffer<AgentQueueBuffer>(targetConnectionEnt);
						var agentQueue = PostUpdateCommands.SetBuffer<AgentQueueBuffer>(targetConnectionEnt);
						agentQueue.CopyFrom(oldQueue);
						agentQueue.Add(new AgentQueueBuffer {Agent = agentEnt});

						timer.TimerType = TimerType.Ticking;
					}
					else
					{
						timer.ChangeToEveryFrame(ref timerState);
					}
				}
			});
		}
	}
}
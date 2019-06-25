using Model.Components;
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
			Entities.ForEach((AgentSpawner spawner, ref Timer timer, ref TimerState timerState,
				ref ConnectionCoord spawnTarget,
				ref ConnectionTarget agentTarget) =>
			{
				if (timerState.CountDown == 0)
				{
					var agent = EntityManager.GetComponentData<Agent>(spawner.Agent);
					var targetConnectionEnt = spawnTarget.Connection;
					var conSpeed = EntityManager.GetComponentData<Connection>(targetConnectionEnt);
					var conLength = EntityManager.GetComponentData<ConnectionLength>(targetConnectionEnt);
					var connectionState = EntityManager.GetComponentData<ConnectionState>(targetConnectionEnt);

					if (connectionState.CouldAgentEnter(ref agent, ref conLength))
					{
						var agentEnt = PostUpdateCommands.Instantiate(spawner.Agent);
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
						PostUpdateCommands.SetComponent(agentEnt, new TimerState //need this so that it can move immediately!
						{
							CountDown = interval,
						});
						
						connectionState.AcceptAgent(ref agent);						
						EntityManager.SetComponentData(targetConnectionEnt, connectionState);
						
						var oldQueue = EntityManager.GetBuffer<AgentQueueBuffer>(targetConnectionEnt);
						var agentQueue = PostUpdateCommands.SetBuffer<AgentQueueBuffer>(targetConnectionEnt);
						agentQueue.CopyFrom(oldQueue);
						agentQueue.Add(new AgentQueueBuffer{Agent = agentEnt});
						
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
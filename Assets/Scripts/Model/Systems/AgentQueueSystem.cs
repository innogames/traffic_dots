using Model.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(PathCacheCommandBufferSystem))]
	public class AgentQueueSystem : JobComponentSystem
	{
		private struct AgentEnterConnection : IJobForEachWithEntity<Agent, ConnectionCoord, 
				ConnectionTarget, Timer, TimerState>
		{
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<ConnectionState> ConnectionStates;
			
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;
			[ReadOnly] public BufferFromEntity<AgentQueueBuffer> AgentQueue;

			public void Execute(Entity entity, int index,
				[ReadOnly] ref Agent agent,
				ref ConnectionCoord coord,
				[ReadOnly] ref ConnectionTarget target,
				ref Timer timer, ref TimerState timerState)
			{
				if (timerState.CountDown == 0)
				{
					if (coord.Connection == target.Connection)
					{
						CommandBuffer.RemoveComponent<ConnectionTarget>(index, entity);
					}
					else
					{
						if (coord.Coord <= 0f) //reach end of connection
						{
							var curConnectionEnt = coord.Connection;
							var startNode = Connections[curConnectionEnt].EndNode;
							var endNode = Connections[target.Connection].StartNode;
							var nextConnectionEnt = startNode == endNode
								? target.Connection
								: Next[startNode][Indexes[endNode].Index].Connection;
							var nextConnection = Connections[nextConnectionEnt];
							var nextState = ConnectionStates[nextConnectionEnt];
							if (nextState.CouldAgentEnter(ref agent, ref nextConnection))
							{
								coord.Connection = nextConnectionEnt;
								coord.Coord = nextState.NewAgentCoord(ref nextConnection);

								timer.Frames = nextState.FramesToEnter(ref nextConnection);
								timerState.CountDown = timer.Frames;

								if (AgentQueue[curConnectionEnt].Length > 0)
								{
									var curConnection = Connections[curConnectionEnt];
									var curState = ConnectionStates[curConnectionEnt];
									curState.ExitLength =
										math.min(curState.ExitLength + agent.Length, curConnection.Length);
									ConnectionStates[curConnectionEnt] = curState;									
								}

								nextState.AcceptAgent(ref agent);
								ConnectionStates[nextConnectionEnt] = nextState;
								
								//a connection never accept two agents going in at the same time => no race condition!
								AgentQueue[nextConnectionEnt].Add(new AgentQueueBuffer{Agent = entity});
							}
							else
							{
								//TODO freeze timer here
							}
						}
					}
				}
			}
		}
		
		private struct AgentMoveForward : IJobForEachWithEntity<Connection, ConnectionState>
		{
			//agent stuffs, no other connection could share an agent ==> no race condition
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<ConnectionCoord> Coords;
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<Timer> Timers;
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<TimerState> TimerStates;			

			[ReadOnly] public BufferFromEntity<AgentQueueBuffer> AgentQueue;
			public void Execute(Entity entity, int index, [ReadOnly] ref Connection connection, ref ConnectionState state)
			{
				if (state.ExitLength > 0) //have gap to fill!
				{
					var queue = AgentQueue[entity];
					for (int i = 0; i < queue.Length - 1; i++)
					{
						var agent = queue[i] = queue[i + 1];
						var agentCoord = Coords[agent.Agent];
						Coords[agent.Agent] = new ConnectionCoord
						{
							Connection = entity,
							Coord = agentCoord.Coord - state.ExitLength,
						};;
						int extraTime = (int) math.ceil(state.ExitLength / connection.Speed);
						var timer = Timers[agent.Agent];
						Timers[agent.Agent] = new Timer
						{
							Frames = timer.Frames + extraTime,
						};
						var timerState = TimerStates[agent.Agent];
						TimerStates[agent.Agent] = new TimerState
						{
							CountDown = timerState.CountDown + extraTime,
						};
					}
					queue.RemoveAt(queue.Length - 1);
					state.EnterLength += state.ExitLength;
					state.ExitLength = 0f;
				}
			}
		}

		private EntityCommandBufferSystem _bufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();
			var connections = GetComponentDataFromEntity<Connection>();
			var connectionStates = GetComponentDataFromEntity<ConnectionState>();
			var indexes = GetComponentDataFromEntity<IndexInNetwork>();
			var coords = GetComponentDataFromEntity<ConnectionCoord>();
			var timers = GetComponentDataFromEntity<Timer>();
			var timerStates = GetComponentDataFromEntity<TimerState>();

			var agentQueue = GetBufferFromEntity<AgentQueueBuffer>();
			var next = GetBufferFromEntity<NextBuffer>();

			var enterConnection = new AgentEnterConnection
			{
				CommandBuffer = commandBuffer,
				Connections = connections,
				ConnectionStates = connectionStates,
				Indexes = indexes,
				AgentQueue = agentQueue,
				Next = next,
			}.Schedule(this, inputDeps);

			var moveForward = new AgentMoveForward
			{
				AgentQueue = agentQueue,
				Coords = coords,
				Timers = timers,
				TimerStates = timerStates,
			}.Schedule(this, enterConnection);
			moveForward.Complete();
			return moveForward;
		}
	}
}
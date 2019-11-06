using Model.Components;
using Model.Components.Buffer;
using Model.Systems.States;
using Unity.Burst;
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
//		[BurstCompile]
		private struct AgentEnterConnection : IJobForEachWithEntity<Agent, ConnectionCoord, TailCoord,
			ConnectionTarget, Timer, TimerState>
		{
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionState> ConnectionStates;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateAdjust> Adjusts;

			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLength> ConLengths;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;
			[ReadOnly] public BufferFromEntity<AgentQueueBuffer> AgentQueue;

			public void Execute(Entity entity, int index,
				[ReadOnly] ref Agent agent,
				ref ConnectionCoord coord,
				ref TailCoord tailCoord,
				[ReadOnly] ref ConnectionTarget target,
				ref Timer timer, ref TimerState timerState)
			{
				if (timerState.CountDown == 0 && timer.TimerType != TimerType.Freezing)
				{
					var curConnectionEnt = coord.Connection;
					var targetConnectionEnt = target.Connection;

					//reach final destination
					if (curConnectionEnt == targetConnectionEnt)
					{
						//remove entity
						CommandBuffer.DestroyEntity(index, entity);
						var curLength = ConLengths[curConnectionEnt];
						var curState = ConnectionStates[curConnectionEnt];
						var curAdjust = Adjusts[curConnectionEnt];
						var curQueue = AgentQueue[curConnectionEnt];
						if (curQueue.Length > 1) //TODO check leave partially
						{
							curAdjust.AgentLeaveThePack(ref agent, ref curLength);
							curAdjust.WillRemoveAgent = true;
							Adjusts[curConnectionEnt] = curAdjust;
						}
						else
						{
							curState.ClearConnection(ref curLength);
							curQueue.Clear(); //the only car here => no race condition
							ConnectionStates[curConnectionEnt] = curState;
						}
//						timer.TimerType = TimerType.Freezing;
					}
					else
					{
						if (coord.Coord <= 0f) //reach end of connection
						{
							var startNode = Connections[curConnectionEnt].EndNode;
							var endNode = Connections[targetConnectionEnt].StartNode;
							var nextConnectionEnt = startNode == endNode
								? targetConnectionEnt
								: Next[startNode][Indexes[endNode].Index].Connection;
							var nextConnection = Connections[nextConnectionEnt];
							var nextLength = ConLengths[nextConnectionEnt];
							var nextConTraffic = ConTraffics[nextConnectionEnt];
							ConnectionState nextState;

							if (nextConTraffic.TrafficType != ConnectionTrafficType.NoEntrance
							    && (nextState = ConnectionStates[nextConnectionEnt]).CouldAgentPartiallyEnter())
							{
								//can enter next connection
								//move distance = nextState.EnterLength
								//old tail pos = agent.Length (because oldCoord = 0)
								//we assume that the agent span at MAX 3 connections!

								//head
								coord.Connection = nextConnectionEnt;
								coord.Coord = nextState.NewAgentCoord(ref nextLength);

								timer.Frames = nextState.FramesToEnter(ref nextConnection);
								timer.TimerType = TimerType.Ticking;
								
								var nextQueue = AgentQueue[nextConnectionEnt];
								nextQueue.Add(entity);

								//old tail
								//old tail stays fully in current connection
								var curLength = ConLengths[curConnectionEnt];
								bool isOldTailInCurCon = agent.Length <= curLength.Length;
								//new tail
								bool isNewTailInLastCon = agent.Length + nextState.EnterLength <= curLength.Length;
								bool isNewTailInNextCon = nextState.EnterLength >= agent.Length;

								var curState = ConnectionStates[curConnectionEnt];
								var curAdjust = new ConnectionStateAdjust
								{
									MoveForward = 0f,
									WillRemoveAgent = true,
								};
								if (isOldTailInCurCon)
								{
									if (isNewTailInNextCon)
									{
										tailCoord.Connection = nextConnectionEnt;

										curAdjust.MoveForward = agent.Length;
										nextState.EnterLength -= agent.Length;
										tailCoord.Coord = curLength.Length - nextState.EnterLength;
									}
									else //new tail in cur con
									{
										tailCoord.Connection = curConnectionEnt;
										tailCoord.Coord = agent.Length - nextState.EnterLength;
										
										curAdjust.MoveForward = nextState.EnterLength;
										nextState.EnterLength = 0f;
									}
								}
								else //old tail in last con
								{
									var curLastCon = tailCoord.Connection;
									var lastAdjust = Adjusts[curLastCon];
									lastAdjust.WillRemoveAgent = false; //because our agent left lastCon already!
									if (isNewTailInLastCon)
									{
										tailCoord.Coord = agent.Length - curLength.Length - nextState.EnterLength;
										
										lastAdjust.MoveForward = nextState.EnterLength;
										nextState.EnterLength = 0f;
										//curState.EnterLength should remain zero
									}
									else
									{
										lastAdjust.MoveForward = agent.Length - curLength.Length;
										if (isNewTailInNextCon)
										{
											tailCoord.Connection = nextConnectionEnt;
											tailCoord.Coord = coord.Coord + agent.Length;
											
											nextState.EnterLength -= agent.Length;
											curState.EnterLength = curLength.Length;
										}
										else //new tail in cur con
										{
											tailCoord.Connection = curConnectionEnt;
											tailCoord.Coord = agent.Length - nextState.EnterLength; 
											
											curState.EnterLength = curLength.Length - tailCoord.Coord;
											nextState.EnterLength = 0f;
										}

										ConnectionStates[curConnectionEnt] = curState;
									}

									Adjusts[curLastCon] = lastAdjust;
								}
								//no other agent can exit or enter similar connections
								ConnectionStates[nextConnectionEnt] = nextState;
								Adjusts[curConnectionEnt] = curAdjust; //remove current agent in all circumstances!
							}
							else
							{
								//can't enter, try again next frame
								timer.ChangeToEveryFrame(ref timerState);
							}
						}
						else
						{
							timer.TimerType = TimerType.Freezing; //waiting to be pull by AgentMoveForward
						}
					}
				}
			}
		}

//		[BurstCompile]
		private struct AgentMoveForward : IJobForEachWithEntity<Connection, ConnectionState, ConnectionLength>
		{
			//agent stuffs, no other connection could share an agent ==> no race condition
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionCoord> Coords;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<TailCoord> TailCoords;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Timer> Timers;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<TimerState> TimerStates;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateAdjust> Adjusts;

			[ReadOnly] public BufferFromEntity<AgentQueueBuffer> AgentQueue;
			[ReadOnly] public ComponentDataFromEntity<Agent> Agents;

			public void Execute(Entity entity, int index, [ReadOnly] ref Connection connection,
				ref ConnectionState state, [ReadOnly] ref ConnectionLength conLen)
			{
				var newAdjust = Adjusts[entity];
				bool willRemoveAgent = newAdjust.WillRemoveAgent;
				float moveForward = newAdjust.MoveForward;
				var queue = AgentQueue[entity];
				if (moveForward > 0f) //have gap to fill!
				{
					for (int i = 0; i < queue.Length - (willRemoveAgent ? 1 : 0); i++)
					{
						AgentQueueBuffer agentBuf;
						if (willRemoveAgent)
						{
							agentBuf = queue[i] = queue[i + 1];
						}
						else
						{
							agentBuf = queue[i];
						}

						var agentEnt = agentBuf.Agent;
						
						var coord = Coords[agentEnt];
						coord.Coord -= moveForward;
						Coords[agentEnt] = coord;

						var tailCoord = TailCoords[agentEnt];
						tailCoord.Coord -= moveForward;
						TailCoords[agentEnt] = tailCoord;

						int extraTime = (int) math.ceil(moveForward / connection.Speed);
						var timer = Timers[agentEnt];
						Timers[agentEnt] = new Timer
						{
							Frames = timer.Frames + extraTime,
							TimerType = TimerType.Ticking,
						};
						var timerState = TimerStates[agentEnt];
						TimerStates[agentEnt] = new TimerState
						{
							CountDown = timerState.CountDown + extraTime,
						};
						
						//this will cause race-condition, because of the reset at the end!
//						var lastCon = LastCons[agentEnt].Connection;
//						if (i == queue.Length - (willRemoveAgent ? 2 : 1) && lastCon != Entity.Null)
//						{
//							if (newCoord + Agents[agentEnt].Length > conLen.Length) //still occupy last con
//							{
//								var lastConAdjust = Adjusts[lastCon];
////								lastConAdjust.NewExitLength = States[lastCon] gap; //TODO!
//								Adjusts[lastCon] = lastConAdjust; //no other agent can cause this, no race condition
//							}
//						}
					}
					state.EnterLength += moveForward; //TODO add cap here!
				}
				if (willRemoveAgent)
				{
					queue.RemoveAt(queue.Length - 1);
				}
				//reset!
				Adjusts[entity] = new ConnectionStateAdjust
				{
					MoveForward = 0f,
					WillRemoveAgent = false,
				};
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
			var conLengths = GetComponentDataFromEntity<ConnectionLength>();
			var agents = GetComponentDataFromEntity<Agent>();
			var connectionStates = GetComponentDataFromEntity<ConnectionState>();
			var adjusts = GetComponentDataFromEntity<ConnectionStateAdjust>();
			var conTraffics = GetComponentDataFromEntity<ConnectionTraffic>();
			var indexes = GetComponentDataFromEntity<IndexInNetwork>();
			var coords = GetComponentDataFromEntity<ConnectionCoord>();
			var tailCoords = GetComponentDataFromEntity<TailCoord>();
			var timers = GetComponentDataFromEntity<Timer>();
			var timerStates = GetComponentDataFromEntity<TimerState>();

			var agentQueue = GetBufferFromEntity<AgentQueueBuffer>();
			var next = GetBufferFromEntity<NextBuffer>();

			var enterConnection = new AgentEnterConnection
			{
				CommandBuffer = commandBuffer,
				Connections = connections,
				ConLengths = conLengths,
				ConnectionStates = connectionStates,
				Adjusts = adjusts,
				ConTraffics = conTraffics,
				Indexes = indexes,
				AgentQueue = agentQueue,
				Next = next,
			}.Schedule(this, inputDeps);

			var moveForward = new AgentMoveForward
			{
				AgentQueue = agentQueue,
				Coords = coords,
				TailCoords = tailCoords,
				Timers = timers,
				TimerStates = timerStates,
				Agents = agents,
				Adjusts = adjusts,
			}.Schedule(this, enterConnection);
			moveForward.Complete();
			return moveForward;
		}
	}
}
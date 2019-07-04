using Model.Components;
using Model.Systems.States;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(PathCacheCommandBufferSystem))]
	public class AgentTrailingSystem : JobComponentSystem
	{
		private EntityCommandBufferSystem _bufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//propose 2: con.pullCord could have more than one value? NO! there is ONLY one gap at a time!
			//
			//first agent move: if agent.moveDist == 0 (prune) && agent.headCord == headCon.len (imply agent.moveDist == 0)
			//agent input:  headCord, tailCon, tailCord 
			//agent in/out: headCon, headCord
			//agent output: moveDist
			//con input:    headCon.len
			//con in/out:   nextCon.EnterLen
			//con output:   tailCon.pullCord, pullDist
			//
			//- agent.moveDist = nextCon.EnterLen
			//- agent.headCon = nextCon
			//- agent.headCord = 0
			//- tailCon.pullCord = agent.tailCord //pullCord CREATE
			//- tailCon.pullDist = moveDist
			//- nextCon.EnterLen -= min(agent.len, moveDist) //EnterLen DECREASE!

			//pull: if agent.moveDist == 0 & headCon.pullDist > 0 & agent.headCord == headCon.pullCord(max one agent per con)
			//agent input:  headCon, headCord, tailCon, tailCord, moveDist
			//agent output: moveDist
			//con input:    len, pullCord, pullDist
			//con output:   pullCord, pullDist
			//
			//- pull = min(headCon.pullDist, headCon.len - agent.headCord)
			//- agent.moveDist += pull
			//- if tailCon != headCon
			//- - headCon.pullCord = headCon.len //pullCord RESET
			//- - headCon.pullDist = 0
			//- tailCon.pullCord = agent.tailCord //pullCord PROPAGATE
			//- tailCon.pullDist = pull

			//last agent move: one match per connection! no clash!
			//agent input: tailCon, tailCord, moveDist
			//con input:   EnterLen, pullCord, pullDist, len
			//con output:  EnterLen, pullCord, pullDist
			//
			//- if (agent.tailCord == tailCon.EnterLen) //last agent
			//- - move = min(tailCon.len - agent.tailCord, agent.moveDist)
			//- - tailCon.EnterLen += move //EnterLen INCREASE!
			//- - tailCon.pullCord += move //PullCord INCREASE! (until conLen)
			//- - tailCon.pullDist -= move

			//move forward: agent.moveDist > 0
			//agent input:  headCon, headCord, tailCon, tailCord, moveDist
			//agent output: headCord, tailCon, tailCord, moveDist
			//con input:    speed, len
			//con output:   none
			//
			//- speed = agent.headCon.speed
			//- move = min (speed, agent.moveDist) //moveDist is already not exceed con.len!
			//- move = min (move, tailCon.len - agent.tailCord) //lose speed artificially :( //could loop here for tailCord!
			//- agent.headCord += move
			//- agent.tailCord += move
			//- agent.moveDist -= move
			//- if (agent.tailCord == tailCon.len) tailCon = next(tailCon)

			//new propose
			//exitAgent = headCord == headCon.len: is a one-frame state!
			//bridgeAgent = headCon != tailCon: is a MULTI-FRAME state! => has a flag

			//these are 3 jobs, can't combine because of race-condition
			//every exitAgent: tailCon.pullForce = min(moveDist, tailCon.len - tailCord), life = 1
			//every non-exit agent: agent.moveDist += headCon.pullForce, agent.pullFlag = true
			//propagate: every bridge agent with pullFlag: tailCon.pullForce = cap(agent.moveDist), life = 2, pullFlag = false

			//decrease all pullForce life, remove one with 0 life!
			//move all agent forward

			//when agent-head enter connection: con.EnterLen -= agent.Length (done)
			//when agent-head leave connection: start taking back part of agent.Length to connection
			//when agent-tail enter connection: begin partial-return process?
			//when agent-tail leave connection: assert(con.EnterLen not contains agent.Length)

			//heuristic: enterLen of connection "inside" an agent does not matter, it's not used by anyone: WRONG! it's used by intersection!

			//propose 1:
			//exit-agent:
			//- agent.force = min(nextCon.EnterLen, agent.tailLen)
			//- nextCon.EnterLen -= agent.len
			//- nextCon.queue.push(agent)
			//connection queue:
			//- if (queue[0].force > 0): queue[all - last].moveDist += force
			//- con.addComp(conForce = queue[0].force)
			//- queue[0].force = 0
			//connection: all con with conForce
			//- queue[last].force = min(conForce, last.tailLen)
			//- con.removeComp(conForce)
			//moveForward
			//- consume moveDist to move headCord and tailCord
			//- if tailCord == tailLen: tailCon.queue.pop, tailCon.EnterLen = min(force, tailLen), tailCon = next(tailCon)

			//propose 2:
			//exit-agent
			//- agent.queue.add(nextCon)
			//- agent.force = min(nextCon.EnterLen, agent.tailLen)
			//- nextCon.EnterLen -= agent.len
			//pull connection: per agent
			//- if agent.force > 0
			//- agent.queue[all].EnterLen += force //CLASH: because some con share agents!

			//propose 3: all adjustments are queued!
			//foreach(agent):
			//- exit-agent: headCord == headLen
			//- if targetCon == headCon: tailCon.pullQ = min(agent.Length, tailLen) //not clash with (B)
			//- else if nextCon.EnterLen > 0
			//- - assert(moveDist == 0)
			//- - agent.headCon = nextCon; agent.headCord = 0; agent.moveDist = nextCon.EnterLen // (C)
			//- - nextCon.EnterLenQ -= agent.len //(A), not clash with (C)
			//- - tailCon.pullQ = min(moveDist, tailLen)  //(B)
			//- non-exit-agent:
			//- - agent.moveDist += min(headCon.pull, headRoom) //avoid (B)
			//- - if bridge-agent: tailCon.pullQ = min(headCon.pull, tailLen) //not collide with (B)
			//- all-agent: moveDist > 0
			//- - headCord += speed; tailCord += speed
			//- - if tailCross: nextTailCon.EnterLenQ = min(agent.moveDist, nextCon.len) //can't clash with (A)
			//foreach(con)
			//- con.pull = con.pullQ
			//- con.pullQ = 0 //this mean pull exist for 1 frame only!
			//- con.EnterLen = con.EnterLenQ + con.pull
			//- con.EnterLenQ = con.EnterLen


			var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();
			var conLens = GetComponentDataFromEntity<ConnectionLengthInt>();
			var connections = GetComponentDataFromEntity<Connection>();
			var conTraffics = GetComponentDataFromEntity<ConnectionTraffic>();
			var conSpeeds = GetComponentDataFromEntity<ConnectionSpeedInt>();
			var indexes = GetComponentDataFromEntity<IndexInNetwork>();
			var states = GetComponentDataFromEntity<ConnectionStateInt>();
			var pulls = GetComponentDataFromEntity<ConnectionPullInt>();
			var pullQs = GetComponentDataFromEntity<ConnectionPullQInt>();
			var next = GetBufferFromEntity<NextBuffer>();

			var agentJob = new AgentJob
			{
				CommandBuffer = commandBuffer,
				Connections = connections,
				ConLens = conLens,
				ConTraffics = conTraffics,
				Indexes = indexes,
				Next = next,
				ConSpeeds = conSpeeds,
				States = states,
				Pulls = pulls,
				PullQs = pullQs,
				NetGroups = GetComponentDataFromEntity<NetworkGroupState>(),
				NetInfos = GetComponentDataFromEntity<NetPathInfo>(),
				Exits = GetComponentDataFromEntity<Exit>(),
				Entrances = GetComponentDataFromEntity<Entrance>(),
			}.Schedule(this, inputDeps);
			var connectionJob = new ConnectionJob().Schedule(this, agentJob);
			connectionJob.Complete(); //for entity remove
			return connectionJob;
		}
#if !CITY_DEBUG
		[BurstCompile]
#endif
		private struct AgentJob : IJobForEachWithEntity<AgentInt, ConnectionTarget, ConnectionTargetState, AgentCordInt,
			AgentStateInt>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;

			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<NetworkGroupState> NetGroups;
			[ReadOnly] public ComponentDataFromEntity<NetPathInfo> NetInfos;
			[ReadOnly] public ComponentDataFromEntity<Exit> Exits;
			[ReadOnly] public ComponentDataFromEntity<Entrance> Entrances;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;
			[ReadOnly] public ComponentDataFromEntity<ConnectionPullInt> Pulls;
			[ReadOnly] public ComponentDataFromEntity<ConnectionSpeedInt> ConSpeeds;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionPullQInt> PullQs;

			private Entity ComputeNextCon(ref Entity curConEnt,
				ref ConnectionTargetState state)
			{
#if CITY_DEBUG
				if ((Next[curConEnt][state.TargetIndex].Connection == Entity.Null))
				{
					int abc = 123;  
				}
#endif
				return Next[curConEnt][state.TargetIndex].Connection;
			}

			private void ComputeTargetState(ref Entity finalTarget, out ConnectionTargetState state, int curNet,
				int curLevel,
				ref Entity curLoc)
			{
				int finalNet = NetGroups[finalTarget].NetworkId;
				int finalLevel = Connections[finalTarget].Level;
				if (curNet == finalNet) //same net with final
				{
					state.NextTarget = finalTarget;
				}
				else //dif net with final
				{
					if (curLevel <= finalLevel) //climb
					{
						state.NextTarget = NetInfos[curLoc].NearestExit;
					}
					else //descend
					{
						state.NextTarget = finalTarget;
						do
						{
							state.NextTarget = NetInfos[state.NextTarget].NearestEntrance;
						} while (curLevel > Exits[state.NextTarget].Level);

						var exit = Exits[state.NextTarget];
						if (curNet != exit.NetIdx) //dif net, climb
						{
							state.NextTarget = NetInfos[state.NextTarget].NearestExit;
						}
					}
				}

				state.TargetIndex = Indexes[state.NextTarget].Index;
			}

			private Entity CheckReachTarget(ref ConnectionTarget target, ref Entity curConEnt,
				ref ConnectionTargetState state)
			{
				var finalTarget = target.Connection;
				if (curConEnt == finalTarget) //reach final target
				{
					return Entity.Null; //done
				}

				return CheckReachTargetTail(ref curConEnt, ref state, ref finalTarget);
			}

			private Entity CheckReachTargetTail(ref Entity curConEnt, ref ConnectionTargetState state,
				ref Entity finalTarget)
			{
				var con = Connections[curConEnt];

				if (con.OnlyNext != Entity.Null)
				{
					return con.OnlyNext;
				}

				if (con.EndNode == state.NextTarget) //reach next target
				{
					int curNet = Entrances[state.NextTarget].NetIdx;
					int curLevel = Entrances[state.NextTarget].Level;
					var curLoc = state.NextTarget;
					ComputeTargetState(ref finalTarget, out state, curNet, curLevel, ref curLoc);
#if CITY_DEBUG
					if ((Next[curLoc][state.TargetIndex].Connection == Entity.Null))
					{
						int abc = 123;  
					}
#endif
					return Next[curLoc][state.TargetIndex].Connection;
				}

				//in the middle of the path
				if (state.NextTarget == Entity.Null) //state not computed yet
				{
					int curNet = NetGroups[curConEnt].NetworkId;
					int curLevel = con.Level;
					ComputeTargetState(ref finalTarget, out state, curNet, curLevel, ref curConEnt);
				}

				return ComputeNextCon(ref curConEnt, ref state);
			}

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref ConnectionTargetState targetState,
				ref AgentCordInt cord,
				ref AgentStateInt agentState)
			{
				var headConEnt = cord.HeadCon;
				int headConLen = ConLens[headConEnt].Length;
				bool endHeadCon = cord.HeadCord >= headConLen;

				var tailConEnt = agentState.TailCon;
				int tailConLen = ConLens[tailConEnt].Length;
				//reached target
				if (endHeadCon) //end of connection
				{
#if CITY_DEBUG
					if (agentState.MoveDist != 0)
					{
						int abc = 123;
					}
#endif
					var nextConEnt = CheckReachTarget(ref target, ref headConEnt, ref targetState);
					if (nextConEnt == Entity.Null) //reach destination!
					{
						CommandBuffer.DestroyEntity(index, agentEnt);
						//create "death pull"
						PullQs[tailConEnt] = new ConnectionPullQInt
						{
							PullQ = math.min(agent.Length, tailConLen - agentState.TailCord),
						};
					}
					else
					{
						var nextTraffic = ConTraffics[nextConEnt];
						var nextState = States[nextConEnt];
						if (nextTraffic.TrafficType != ConnectionTrafficType.NoEntrance &&
						    nextState.EnterLen > 0)
						{
							int moveDist = nextState.EnterLen;
#if CITY_DEBUG
							if (moveDist > ConLens[nextConEnt].Length)
							{
								int abc = 123;
							}
#endif
							//update agent
							agentState.MoveDist = moveDist;
							cord.HeadCon = nextConEnt;
							cord.HeadCord = 0;

							//create pull
							PullQs[tailConEnt] = new ConnectionPullQInt
							{
								PullQ = math.min(moveDist, tailConLen - agentState.TailCord),
							};

							//update next connection
							nextState.EnterLen -= agent.Length; //it's necessary to stay negative!
							States[nextConEnt] = nextState;
						}
						else
						{
							//can't enter connection, do nothing, NOT ADD FROZEN
							//next frame will check for connection enter again
						}
					}
				}
				else //middle of connection
				{
					var headConPull = Pulls[headConEnt];
					int pullForce = headConPull.Pull;
					if (cord.HeadCord + agentState.MoveDist + pullForce > headConLen)
					{
						//because the pull is 1 frame delay!
						pullForce = headConLen - cord.HeadCord - agentState.MoveDist;
					}

					if (pullForce > 0)
					{
						agentState.MoveForce += pullForce;
#if CITY_DEBUG
						if (cord.HeadCord + agentState.MoveDist + agentState.MoveForce > headConLen)
						{
							int abc = 123;
						}
#endif
					}
				}

				if (agentState.MoveDist > 0)
				{
					//move forward here
					int speed = ConSpeeds[cord.HeadCon].Speed;
					int moveDist = math.min(speed, agentState.MoveDist);

					//because of 1 frame delay could lead to overshoot
					moveDist = math.min(moveDist, headConLen - cord.HeadCord);

//					cord.HeadCord += moveDist;

					while (moveDist > 0)
					{
						int curDist = math.min(moveDist, tailConLen - agentState.TailCord);

						agentState.TailCord += curDist;
						cord.HeadCord += curDist;
#if CITY_DEBUG
						if (cord.HeadCord > headConLen)
						{
							int abc = 123;
						}
#endif
						agentState.MoveDist -= curDist;
#if CITY_DEBUG
						if (agentState.MoveDist < 0)
						{
							int abc = 123;
						}
#endif
						if (agentState.TailCord == tailConLen)
						{
							var nextTailCon =
								CheckReachTargetTail(ref tailConEnt, ref targetState, ref target.Connection);
							agentState.TailCon = nextTailCon;
							agentState.TailCord = 0; //this means no other agent can be behind this one in nextCon!
							int nextLen = ConLens[nextTailCon].Length;
							States[nextTailCon] = new ConnectionStateInt
							{
								EnterLen = math.min(agentState.MoveDist + agentState.MoveForce, nextLen),
							};
						}

						moveDist = 0; // use the one below, but have to clear EnterLen and Pull
//						moveDist -= curDist;
					}
				}
				else
				{
					agentState.MoveDist = agentState.MoveForce;
					agentState.MoveForce = 0;
					if (headConEnt != tailConEnt && agentState.MoveDist > 0) //bridge-agent
					{
						PullQs[tailConEnt] = new ConnectionPullQInt
						{
							PullQ = math.min(agentState.MoveDist, tailConLen - agentState.TailCord),
						};
					}
				}
			}
		}

#if !CITY_DEBUG
		[BurstCompile]
#endif
		private struct ConnectionJob : IJobForEach<ConnectionStateInt, ConnectionPullInt,
			ConnectionPullQInt, ConnectionLengthInt>
		{
			public void Execute(ref ConnectionStateInt state, ref ConnectionPullInt pull,
				ref ConnectionPullQInt pullQ,
				[ReadOnly] ref ConnectionLengthInt conLen)
			{
				pull.Pull = pullQ.PullQ;
				pullQ.PullQ = 0;
				state.EnterLen += pull.Pull;

				if (state.EnterLen > conLen.Length)
				{
					state.EnterLen = conLen.Length;
				}
			}
		}
	}
}
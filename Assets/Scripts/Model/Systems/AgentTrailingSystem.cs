using Model.Components;
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
	public class AgentTrailingSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//propose: race condition!
			//enter connection:
			//- only agent at the end of con
			//=> both curCon and nextCon are unique. nextCon could collide with curCon
			//- input: nextCon.state
			//- input: nextCon.traffic
			//- input: nextCon.trail
			//- input: curCon.state
			//- in/out: agent.coord, agent.state
			//- output: nextCon.trail
			//- output: nextCon.state
			//- output: curCon.state, only if curCon has 1 agent --> will CLASH with nextCon
			//- scenario: X enter BC, Y is leaving BC
			//- X output to BC.EnterLen
			//- Y output to BC.EnterLen => clash!
			//enter connection.chill: for agent with moveDist == 0 & not at end of con: do nothing!
			//pull
			//- only agent with moveDist > 0 & not end of con & agent.state == pull
			//- input: agent.trail
			//- in/out: agent.coord, agent.state
			//- output: trailAgent.state -> no clash with agent.state?
			//- - apply to only trailAgent.state != pull => good!
			//- - set trailAgent.state to pull => bad!
			//- because if an agent state is Pull, it just has a new coord (from pulled, or end of con) => no need to pull it!
			//move forward
			//- only agent with moveDist > 0 & not end of con
			//- in/out: agent.coord, agent.trail, agent.state
			//- no pointer output!
			
			//propose: race condition!
			//enter connection: per agent: at end con
			//- output: nextCon.EnterSpace: occupy space
			//- input: nextCon.trailAgent
			//- output: nextCon.trailAgent.trailAgent = agent
			//- output: nextCon.trailAgent
			//- output: agent.trailAgent.moveDist
			//- output: agent.moveDist
			//- output: agent.trailAgent: CLASH with nextCon.trailAgent
			//pull: per agent: with headCon.moveDist > 0
			//- agent.moveDist+
			//- output: tailCon.moveDist => got clean up!
			//clear: per con with con.moveDist > 0
			//- con.EnterLen += moveDist
			//- con.moveDist = 0
			
			//propose 2: con.pullCord could have more than one value? NO! there is ONLY one gap at a time!
			//first agent move: if agent.moveDist == 0 (prune) && agent.headCord == headCon.len (imply agent.moveDist == 0)
			//- in/out: nextCon.EnterLen
			//- in/out: agent.headCon, moveDist
			//- output: tailCon.pullCord, hasPull
			//- agent.moveDist = nextCon.EnterLen
			//- tailCon.pullCord = agent.tailCord //pullCord CREATE
			//- tailCon.pullDist = moveDist
			//- agent.headCon = nextCon
			//- nextCon.EnterLen -= min(agent.len, nextCon.EnterLen) //EnterLen DECREASE!
			
			//pull: if agent.moveDist == 0 & headCon.pullDist > 0 & agent.headCord == headCon.pullCord(max one agent per con)
			//- input: headCon.len, headCon.pullCord
			//- input: agent.headCord
			//- in/out: agent.moveDist
			//- output: tailCon.pullCord
			//- pull = min(headCon.pullDist, headCon.len - agent.headCord)
			//- agent.moveDist += pull
			//- if (tailCon != headCon) //pullCord CREATE
			//- - tailCon.pullCord = agent.tailCord
			//- - tailCon.pullDist = pull
			//- - headCon.pullCord = headCon.EnterLen //which is 0 ==> to clear
			//- else
			//- - headCon.pullCord = agent.tailCord
			
			//last agent move: one match per connection! no clash!
			//- if (agent.tailCord == tailCon.EnterLen) //last agent
			//- - move = min(tailCon.len - agent.tailCord, agent.moveDist)
			//- - tailCon.EnterLen += move //EnterLen INCREASE!
			//- - tailCon.pullCord += move //PullCord INCREASE! (until conLen)
			//- - tailCon.pullDist -= move

			//move: agent.moveDist > 0
			//- in/out: agent.headCord, tailCord, moveDist
			//- speed = agent.headCon.speed
			//- move = min (speed, agent.moveDist) //moveDist is already not exceed con.len!
			//- move = min (move, tailCon.len - agent.tailCord) //lose speed artificially :( //could loop here for tailCord!
			//- agent.headCord += move
			//- agent.tailCord += move
			//- agent.moveDist -= move
			//- if (agent.tailCord == tailCon.len) tailCon = next(tailCon)
			
			var moveJob = new EnterConnection
			{
				AgentStates = GetComponentDataFromEntity<AgentStateInt>(),
				ConLens = GetComponentDataFromEntity<ConnectionLengthInt>(),
				Connections = GetComponentDataFromEntity<Connection>(),
				ConTraffics = GetComponentDataFromEntity<ConnectionTraffic>(),
				Indexes = GetComponentDataFromEntity<IndexInNetwork>(),
				States = GetComponentDataFromEntity<ConnectionStateInt>(),
				TrailAgents = GetComponentDataFromEntity<TrailAgent>(),
				Next = GetBufferFromEntity<NextBuffer>(),
			}.Schedule(this, inputDeps);

			return moveJob;
		}

		[BurstCompile]
		private struct EnterConnection : IJobForEachWithEntity<AgentInt, ConnectionTarget, AgentCoordInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<TrailAgent> TrailAgents;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCoordInt coord,
				ref AgentStateInt agentState)
			{
				var curConEnt = coord.Connection;
				int curConLen = ConLens[curConEnt].Length;
				bool endOfCon = coord.CurCoord == curConLen;
				//because moveDist may overshoot conLen and remain positive at end con
				if (agentState.MoveDist == 0 || endOfCon)
				{
					//reached target
					if (endOfCon) //end of connection
					{
						//compute next path
						var nextConEnt = ComputeNextCon(ref target, ref curConEnt);

						var nextTraffic = ConTraffics[nextConEnt];
						var nextState = States[nextConEnt];
						if (nextTraffic.TrafficType != ConnectionTrafficType.NoEntrance &&
						    nextState.EnterLength > 0)
						{
							//update next connection
							int moveDist = nextState.EnterLength;
							agentState.MoveDist = moveDist; //also discard excess modeDist from last con!
							agentState.State = AgentState.Pull;
							
							//TODO perform pulling here, because the pulled agent can't be first in any connection!
							
							nextState.EnterLength = math.max(0, nextState.EnterLength - agent.Length);

							//update trail agent
							var nextStateTrailAgentEnt = nextState.TrailAgent;
							if (nextStateTrailAgentEnt != Entity.Null)
							{
								var trailAgent = TrailAgents[nextStateTrailAgentEnt];
								trailAgent.Agent = agentEnt;
								TrailAgents[nextStateTrailAgentEnt] = trailAgent; //no clash here
							}

							nextState.TrailAgent = agentEnt;

							States[nextConEnt] = nextState; //may clash here!

							//update cur connection
							var state = States[curConEnt];
							state.EnterLength += math.min(moveDist, agent.Length);
							States[curConEnt] = state; //no clash
						}
						else
						{
							//can't enter connection, do nothing, NOT ADD FROZEN
							//next frame will check for connection enter again
						}
					}
					else //middle of connection, and no fuel
					{
						//stand still, do nothing!, consider adding Frozen?
					}
				}
			}

			private Entity ComputeNextCon(ref ConnectionTarget target, ref Entity curConEnt)
			{
				var startNode = Connections[curConEnt].EndNode;
				var targetConnectionEnt = target.Connection;
				var endNode = Connections[targetConnectionEnt].StartNode;
				var nextConEnt = startNode == endNode
					? targetConnectionEnt
					: Next[startNode][Indexes[endNode].Index].Connection;
				return nextConEnt;
			}
		}

		[BurstCompile]
		private struct MovingForward : IJobForEach<AgentInt, ConnectionTarget, AgentCoordInt, TrailAgent,
			AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			private Entity ComputeNextCon(ref ConnectionTarget target, ref Entity curConEnt)
			{
				var startNode = Connections[curConEnt].EndNode;
				var targetConnectionEnt = target.Connection;
				var endNode = Connections[targetConnectionEnt].StartNode;
				var nextConEnt = startNode == endNode
					? targetConnectionEnt
					: Next[startNode][Indexes[endNode].Index].Connection;
				return nextConEnt;
			}

			public void Execute([ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCoordInt coord,
				ref TrailAgent trailAgent,
				ref AgentStateInt agentState)
			{
				var curConEnt = coord.Connection;
				int curConLen = ConLens[curConEnt].Length;
				bool endOfCon = coord.CurCoord == curConLen;
				//because moveDist may overshoot conLen and remain positive at end con
				if (agentState.MoveDist > 0 && !endOfCon)
				{
					//move forward
					int moveDist = math.min(agentState.MoveDist, agent.Speed);
					agentState.MoveDist -= moveDist;
					coord.CurCoord += moveDist;

					//update tail
					var tailConEnt = coord.TailCon;
					int tailConLen = ConLens[tailConEnt].Length;
					int newTail = coord.CurTail + moveDist - tailConLen; //in case it cross the conLen
					if (newTail >= 0)
					{
						var nextTailCon = ComputeNextCon(ref target, ref tailConEnt);
						coord.TailCon = nextTailCon;

						trailAgent.Agent = Entity.Null; //tail get to new connection, now has no trail!
						coord.CurTail = newTail;
					}
					else
					{
						coord.CurTail += moveDist;
					}
				}
			}
		}

		[BurstCompile]
		private struct Pulling : IJobForEachWithEntity<AgentInt, ConnectionTarget, AgentCoordInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<TrailAgent> TrailAgents;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<AgentStateInt> AgentStates;

			private Entity ComputeNextCon(ref ConnectionTarget target, ref Entity curConEnt)
			{
				var startNode = Connections[curConEnt].EndNode;
				var targetConnectionEnt = target.Connection;
				var endNode = Connections[targetConnectionEnt].StartNode;
				var nextConEnt = startNode == endNode
					? targetConnectionEnt
					: Next[startNode][Indexes[endNode].Index].Connection;
				return nextConEnt;
			}

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCoordInt coord,
				ref AgentStateInt agentState)
			{
				var curConEnt = coord.Connection;
				int curConLen = ConLens[curConEnt].Length;
				bool endOfCon = coord.CurCoord == curConLen;
				//because moveDist may overshoot conLen and remain positive at end con
				if (agentState.MoveDist > 0 && !endOfCon)
				{
					//pull trail
					if (agentState.State == AgentState.Pull) //can pull
					{
						var trailAgent = TrailAgents[agentEnt];
						if (trailAgent.Agent != Entity.Null)
						{
							var trailAgentState = AgentStates[trailAgent.Agent];
							trailAgentState.MoveDist += agentState.MoveDist; //NOTE this may overshoot the tailConLen!
							trailAgentState.State = AgentState.Pull; //propagate the pull
							AgentStates[trailAgent.Agent] = trailAgentState; //CLASH with agentState assignment!
						}

						agentState.State = AgentState.Move; //can no longer pull
					}
				}
			}
		}
	}
}
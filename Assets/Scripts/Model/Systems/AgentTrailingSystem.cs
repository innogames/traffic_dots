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
			var moveJob = new MoveCompute
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
		private struct
			MoveCompute : IJobForEachWithEntity<AgentInt, ConnectionTarget, AgentCoordInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<TrailAgent> TrailAgents;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<AgentStateInt> AgentStates;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCoordInt coord)
			{
				var curConEnt = coord.Connection;
				int curConLen = ConLens[curConEnt].Length;
				var agentState = AgentStates[agentEnt];
				bool endOfCon = coord.CurCoord == curConLen;
				if (agentState.MoveDist == 0 || endOfCon) //because moveDist may overshoot conLen and remain positive at end con
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
							nextState.EnterLength = math.max(0, nextState.EnterLength - agent.Length);
							
							//update trail agent
							var nextStateTrailAgentEnt = nextState.TrailAgent;
							if (nextStateTrailAgentEnt != Entity.Null)
							{
								var trailAgent = TrailAgents[nextStateTrailAgentEnt];
								trailAgent.Agent = agentEnt;
								TrailAgents[nextStateTrailAgentEnt] = trailAgent;
							}
							nextState.TrailAgent = agentEnt;
							
							States[nextConEnt] = nextState;

							//update cur connection
							var state = States[curConEnt];
							state.EnterLength += math.min(moveDist, agent.Length);
							States[curConEnt] = state;

							AgentStates[agentEnt] = agentState;
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
				else
				{
					//pull trail
					var trailAgent = TrailAgents[agentEnt];
					if (agentState.State == AgentState.Pull) //can pull
					{
						if (trailAgent.Agent != Entity.Null)
						{
							var trailAgentState = AgentStates[trailAgent.Agent];
							trailAgentState.MoveDist += agentState.MoveDist; //NOTE this may overshoot the tailConLen!
							trailAgentState.State = AgentState.Pull; //propagate the pull
							AgentStates[trailAgent.Agent] = trailAgentState;
						}
						agentState.State = AgentState.Move; //can no longer pull
					}

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
						TrailAgents[agentEnt] = trailAgent;
						coord.CurTail = newTail;
					}
					else
					{
						coord.CurTail += moveDist;
					}
					AgentStates[agentEnt] = agentState;
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
	}
}
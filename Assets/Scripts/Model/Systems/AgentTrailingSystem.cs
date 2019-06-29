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
			//- tailCon.pullCord = agent.tailCord //pullCord CREATE
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

			var conLens = GetComponentDataFromEntity<ConnectionLengthInt>();
			var connections = GetComponentDataFromEntity<Connection>();
			var conTraffics = GetComponentDataFromEntity<ConnectionTraffic>();
			var conSpeeds = GetComponentDataFromEntity<ConnectionSpeedInt>();
			var indexes = GetComponentDataFromEntity<IndexInNetwork>();
			var states = GetComponentDataFromEntity<ConnectionStateInt>();
			var pulls = GetComponentDataFromEntity<ConnectionPullInt>();
			var next = GetBufferFromEntity<NextBuffer>();
			
			var firstAgentMove = new FirstAgentMove
			{
				Connections = connections,
				ConLens = conLens,
				ConTraffics = conTraffics,
				Indexes = indexes,
				Next = next,
				States = states,
				Pulls = pulls,
			}.Schedule(this, inputDeps);

			var pullPropagate = new PullPropagate
			{
				ConLens = conLens,
				Pulls = pulls,
			}.Schedule(this, firstAgentMove);

			var lastAgentMove = new LastAgentMove
			{
				ConLens = conLens,
				Pulls = pulls,
				States = states,
			}.Schedule(this, pullPropagate);

			var moveForward = new MoveForward
			{
				Connections = connections,
				ConLens = conLens,
				Indexes = indexes,
				Next = next,
				ConSpeeds = conSpeeds,
				Pulls = pulls,
			}.Schedule(this, lastAgentMove);
			
			return moveForward;
		}

		[BurstCompile]
		private struct FirstAgentMove : IJobForEachWithEntity<AgentInt, ConnectionTarget, AgentCordInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<ConnectionTraffic> ConTraffics;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionPullInt> Pulls;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCordInt cord,
				ref AgentStateInt agentState)
			{
				if (agentState.MoveDist == 0)
				{
					var headConEnt = cord.HeadCon;
					int headConLen = ConLens[headConEnt].Length;
					bool endHeadCon = cord.HeadCord == headConLen;
					//because moveDist may overshoot conLen and remain positive at end con

					//reached target
					if (endHeadCon) //end of connection
					{
						//TODO check reach final target!
						//compute next path
						var nextConEnt = ComputeNextCon(ref target, ref headConEnt);

						var nextTraffic = ConTraffics[nextConEnt];
						var nextState = States[nextConEnt];
						if (nextTraffic.TrafficType != ConnectionTrafficType.NoEntrance &&
						    nextState.EnterLength > 0)
						{
							int moveDist = nextState.EnterLength;
							//update agent
							agentState.MoveDist = moveDist;
							cord.HeadCon = nextConEnt;
							cord.HeadCord = 0;

							//create pull
							Pulls[agentState.TailCon] = new ConnectionPullInt
							{
								PullCord = agentState.TailCord,
								PullDist = moveDist,
							};

							//update next connection
							nextState.EnterLength = math.max(0, nextState.EnterLength - agent.Length);
							States[nextConEnt] = nextState; //may clash here!
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
		private struct PullPropagate : IJobForEachWithEntity<AgentInt, AgentCordInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionPullInt> Pulls;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref AgentCordInt cord,
				ref AgentStateInt agentState)
			{
				if (agentState.MoveDist == 0)
				{
					var headConEnt = cord.HeadCon;
					var headConPull = Pulls[headConEnt];
					if (headConPull.PullDist > 0 && cord.HeadCord == headConPull.PullCord)
					{
						int pullDist = math.min(headConPull.PullDist, ConLens[headConEnt].Length - cord.HeadCord);
						agentState.MoveDist += pullDist;
						Pulls[agentState.TailCon] = new ConnectionPullInt
						{
							PullCord = agentState.TailCord,
							PullDist = pullDist,
						};
					}
				}
			}
		}

		[BurstCompile]
		private struct LastAgentMove : IJobForEachWithEntity<AgentInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionPullInt> Pulls;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref AgentStateInt agentState)
			{
				if (agentState.MoveDist > 0)
				{
					var tailConEnt = agentState.TailCon;
					var tailConState = States[tailConEnt];
					if (agentState.TailCord == tailConState.EnterLength) //last agent
					{
						int moveDist = math.min(ConLens[tailConEnt].Length - agentState.TailCord, agentState.MoveDist);
						tailConState.EnterLength += moveDist;
						States[tailConEnt] = tailConState;

						var tailPull = Pulls[tailConEnt];
						tailPull.PullCord += moveDist;
						tailPull.PullDist -= moveDist;
						Pulls[tailConEnt] = tailPull;
					}
				}
			}

		[BurstCompile]
		private struct MoveForward : IJobForEachWithEntity<AgentInt, ConnectionTarget, AgentCordInt, AgentStateInt>
		{
			[ReadOnly] public ComponentDataFromEntity<Connection> Connections;
			[ReadOnly] public ComponentDataFromEntity<ConnectionLengthInt> ConLens;
			[ReadOnly] public ComponentDataFromEntity<IndexInNetwork> Indexes;
			[ReadOnly] public BufferFromEntity<NextBuffer> Next;
			
			[ReadOnly] public ComponentDataFromEntity<ConnectionSpeedInt> ConSpeeds;

//			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionStateInt> States;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<ConnectionPullInt> Pulls;

			public void Execute(Entity agentEnt, int index,
				[ReadOnly] ref AgentInt agent,
				[ReadOnly] ref ConnectionTarget target,
				ref AgentCordInt cord,
				ref AgentStateInt agentState)
			{
				if (agentState.MoveDist > 0)
				{
					int speed = ConSpeeds[cord.HeadCon].Speed;
					int moveDist = math.min(speed, agentState.MoveDist);
					
					cord.HeadCord += moveDist;

					while (moveDist > 0)
					{
						var tailConEnt = agentState.TailCon;
						int tailConLen = ConLens[tailConEnt].Length;
						int curDist = math.min(moveDist, tailConLen - agentState.TailCord);

						agentState.TailCord += curDist;
						agentState.MoveDist -= curDist;

						if (agentState.TailCord == tailConLen)
						{
							agentState.TailCon = ComputeNextCon(ref target, ref tailConEnt);
							agentState.TailCord = 0;
						}

						moveDist = 0; // use the one below, but have to clear EnterLen and Pull
//						moveDist -= curDist;
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
	}
}
using Model.Components;
using Model.Systems.States;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEditor.UIElements;
using Network = Model.Systems.States.Network;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateBefore(typeof(PathCacheCommandBufferSystem))]
	public class NetworkGroupCreationSystem : JobComponentSystem
	{
		[BurstCompile]
		[ExcludeComponent(typeof(NetworkGroup))]
		private struct OutConAndInCon : IJobForEachWithEntity<Connection>
		{
			public NativeMultiHashMap<Entity, Entity>.Concurrent OutCons;
			public NativeMultiHashMap<Entity, Entity>.Concurrent InCons;
			public NativeQueue<Entity>.Concurrent NewCons;

			public void Execute(Entity conEnt, int index, [ReadOnly] ref Connection connection)
			{
				OutCons.Add(connection.StartNode, conEnt);
				InCons.Add(connection.EndNode, conEnt);
				NewCons.Enqueue(conEnt);
			}
		}

		private NativeMultiHashMap<Entity, Entity> _outCons;
		private NativeMultiHashMap<Entity, Entity> _inCons;
		private NativeQueue<Entity> _newCons;
		private NativeHashMap<Entity, int> _conToNets;
		private NativeQueue<Entity> _bfsOpen;
		private NativeList<Entity> _network;
		private NativeHashMap<Entity, int> _entrances;
		private NativeHashMap<Entity, int> _exits;

		private int _networkCount;
		private EntityArchetype _networkArchetype;
		private EntityCommandBufferSystem _bufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(Network)),
				new ComponentType(typeof(NetAdjust)));
			_bufferSystem = World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
			_outCons = new NativeMultiHashMap<Entity, Entity>(SystemConstants.MapNodeSize, Allocator.Persistent);
			_inCons = new NativeMultiHashMap<Entity, Entity>(SystemConstants.MapNodeSize, Allocator.Persistent);
			_conToNets = new NativeHashMap<Entity, int>(SystemConstants.MapConnectionSize, Allocator.Persistent);
			_newCons = new NativeQueue<Entity>(Allocator.Persistent);
			_bfsOpen = new NativeQueue<Entity>(Allocator.Persistent);
			_network = new NativeList<Entity>(SystemConstants.MapConnectionSize, Allocator.Persistent);
			_entrances = new NativeHashMap<Entity, int>(SystemConstants.NetworkNodeSize, Allocator.Persistent);
			_exits = new NativeHashMap<Entity, int>(SystemConstants.NetworkNodeSize, Allocator.Persistent);
			_networkCount = 0;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_outCons.Dispose();
			_inCons.Dispose();
			_conToNets.Dispose();
			_newCons.Dispose();
			_bfsOpen.Dispose();
			_network.Dispose();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//coloring algo
			//loop through all connection: MultiHashMap<node, out_con_of_node>, MultiHashMap<node, in_con_of_node>
			//start with a node
			//breath first search with its in_con & out_con, add all the node to visited node, the con to one color group
			//all con must have the same level as the first con selected!
			//repeat with remaining nodes & con

			//a node connecting two connections of different levels is an exit node
			//nothing can have two IndexInNetwork comp
			//can a node belong to two networks? and have two indexes?
			
			//propose 1: treat exit & entrance nodes differently
			//entrance: contain a Next list to all network nodes
			//exit: every node has a Next to every exit node (indexed differently)
			//need a way to connect an exit of this network to an entrance of another
			//
			//path finding algo
			//target:
			//- finalTarget: conEnt
			//- nextTarget: entity
			//- targetIdx: int
			//- curLoc: entity
			//- curLevel: int
			//- curNet: int
			//
			// NetworkGroup[connection].Index
			// Entrances[node].NetId
			// Exits[node].NetId
			//
			// Connection[connection].Level
			// Entrances[node].Level
			// Exits[node].Level
			//
			//curNet = curCon.Network
			//curLevel = curCon.Level
			//curLoc = curCon
			//
			//if curCon == finalTarget: done
			//elseif curCon.endNode == nextTarget:
			//  curNet = Entrances[nextTarget].NetId
			//  curLevel = Entrances[nextTarget].Level
			//  curLoc = nextTarget
			//  find_target
			
			//if curNet == finalTarget.Network: targetIdx = Indices[finalTarget]
			//else
			//  if curLevel <= finalTarget.Level //climb
			//    nextTarget = curLoc.exit
			//    targetIdx = Exits[nextTarget].Idx
			//  else //descend
			//    nextTarget = finalTarget
			//    do
			//      nextTarget = finalTarget.entrance
			//    while (curLevel > Exits[nextTarget].Level)
			//    var exitInfo = Exits[nextTarget]
			//    if curNet == exitInfo.NetId
			//      targetIdx = exitInfo.Idx
			//    else //climb
			//      nextTarget = nextTarget.exit
			//      targetIdx = Exits[nextTarget].Idx
			//Next[curCon][targetIdx]



			//propose 2: treat exit & entrance like every other con in the network
			//nearest_exit or entrance will have to choose 1 among multiples (also happens with node, but less)
			//the exit/entrance con is still in the same network, it has to be an OnlyNext con to push the agent
			//to the other network => can't have entrance as intersection! must buffer it with a straight road!
			
			//propose 3: create a new connection with startNode == endNode == exitNode
			//this is not good!

			//compute direct pointer: for connection with 1 exit
			//scan through node with 1 out_con_node: set all connection in in_con_node to has direct pointer

			//compute index in network
			//all con without direct pointer will be indexed incrementally based on network
			//follow the direct_pointer to compute combined-distance to Dist array
			//compute Dist & Next 

			//compute node.Exit = nearest_exit_node, node.Entrance = nearest_entrance_node
			//during computation of Dist, record the smallest Dist[i,j] to Exit[i] if j is an exit node

			//path finding:
			//conTarget:
			//- finalTarget: connection
			//- nextNode: Null
			//
			//if Direct[node] != Entity.Null => use Direct[node]
			//if node == conTarget.nextNode => conTarget.nextNode = null
			//if conTarget.nextNode == Null
			//  nextNode = finalTarget.endNode
			//  while (node.network != nextNode.network)
			//    if node.Level == nextNode.Level => nextNode = node.exit; break
			//    elseif node.Level > nextNode.Level => nextNode = nextNode.entrance
			//  conTarget.nextNode = nextNode
			//use Next[node][IndexInNetwork[conTarget.nextNode]]

			_outCons.Clear();
			_inCons.Clear();
			_newCons.Clear();
			var inout = new OutConAndInCon
			{
				OutCons = _outCons.ToConcurrent(),
				InCons = _inCons.ToConcurrent(),
				NewCons = _newCons.ToConcurrent(),
			}.Schedule(this, inputDeps);

			inout.Complete();

			if (_newCons.Count > 0)
			{
				_conToNets.Clear();
				_bfsOpen.Clear();
				while (_newCons.Count > 0)
				{
					var newCon = _newCons.Dequeue();
					int level = EntityManager.GetComponentData<Connection>(newCon).Level;
					if (_conToNets.TryGetValue(newCon, out int _)) continue; //already has net group!
					//breath-first-search here
					_networkCount++;

					_network.Clear();
					_entrances.Clear();
					_exits.Clear();

					_conToNets.TryAdd(newCon, _networkCount);
					_bfsOpen.Enqueue(newCon);
					_network.Add(newCon);

					while (_bfsOpen.Count > 0)
					{
						var curConEnt = _bfsOpen.Dequeue();
						var connection = EntityManager.GetComponentData<Connection>(curConEnt);
						BFS(ref _outCons, ref _network, connection.EndNode, EntityManager, level, true, ref _exits);
						BFS(ref _inCons, ref _network, connection.StartNode, EntityManager, level, false,
							ref _entrances);
					}

					var networkEnt = EntityManager.CreateEntity(_networkArchetype);
					EntityManager.SetComponentData(networkEnt, new Network
					{
						Index = _networkCount,
					});

					//add NetworkGroup & assign OnlyNext
					for (int i = 0; i < _network.Length; i++)
					{
						var conEnt = _network[i];
						EntityManager.AddSharedComponentData(conEnt, new NetworkGroup
						{
							NetworkId = networkEnt.Index,
						});

						//assign OnlyNext
						if (!EntityManager.HasComponent<Target>(conEnt)
						) //target will always participate network indexes
						{
							var connection = EntityManager.GetComponentData<Connection>(conEnt);
							int count = 0;
							Entity onlyNext = Entity.Null;
							if (_outCons.TryGetFirstValue(connection.EndNode, out onlyNext, out var it))
							{
								do
								{
									count++;
									if (count > 1) break;
								} while (_outCons.TryGetNextValue(out _, ref it));
							}

							if (count == 1)
							{
								connection.OnlyNext = onlyNext;
								EntityManager.SetComponentData(conEnt, connection);
							}
						}
					}

					//compute NetAdjust
//					var buffer = EntityManager.GetBuffer<NetAdjust>(networkEnt);
//					for (int i = 0; i < _network.Length; i++)
//					{
//						var conEnt = _network[i];
//						var connection = EntityManager.GetComponentData<Connection>(conEnt);
//						var conLen = EntityManager.GetComponentData<ConnectionLengthInt>(conEnt);
//						var conSpeed = EntityManager.GetComponentData<ConnectionSpeedInt>(conEnt);
//						buffer.Add(new NetAdjust
//						{
//							Connection = conEnt,
//							Cost = (float)conLen.Length / conSpeed.Speed,
//							StartNode = connection.StartNode,
//							EndNode = connection.EndNode,
//							OnlyNext = connection.OnlyNext,
//						});
//					}
					var networkCache = NetworkCache.Create(networkEnt);
					for (int i = 0; i < _network.Length; i++)
					{
						var conEnt = _network[i];
						var connection = EntityManager.GetComponentData<Connection>(conEnt);
						var conLen = EntityManager.GetComponentData<ConnectionLengthInt>(conEnt);
						var conSpeed = EntityManager.GetComponentData<ConnectionSpeedInt>(conEnt);
						networkCache.AddConnection(connection.StartNode, connection.EndNode,
							(float) conLen.Length / conSpeed.Speed, conEnt, connection.OnlyNext);
					}

					var entrances = _entrances.GetKeyArray(Allocator.Temp);
					//add entrances
					for (int i = 0; i < entrances.Length; i++)
					{
						var node = entrances[i];
						EntityManager.AddComponentData(node, new Entrance
						{
							NetIdx = networkEnt.Index,
							Level = level,
						});
					}

					var exits = _exits.GetKeyArray(Allocator.Temp);
					int conCount = networkCache.ConnectionCount();
					//add exits
					for (int i = 0; i < exits.Length; i++)
					{
						var node = exits[i];
						EntityManager.AddComponentData(node, new Exit
						{
							NetIdx = networkEnt.Index,
							Level = level,
						});
						EntityManager.AddComponentData(node, new IndexInNetwork
						{
							Index = i + conCount,
						});
					}

					networkCache.Compute2(EntityManager, ref entrances, ref exits);
					networkCache.Dispose();
					entrances.Dispose();
					exits.Dispose();
				}
			}

			return inout;
		}

		private void BFS(ref NativeMultiHashMap<Entity, Entity> cons,
			ref NativeList<Entity> network, Entity curNode,
			EntityManager entityManager, int level, bool isOut, ref NativeHashMap<Entity, int> netTravelNodes)
		{
			if (cons.TryGetFirstValue(curNode, out var inoutCon, out var it))
			{
				do
				{
					var con = entityManager.GetComponentData<Connection>(inoutCon);
					if (con.Level != level)
					{
						netTravelNodes.TryAdd(isOut ? con.StartNode : con.EndNode, 0);
						continue;
					}
					if (_conToNets.TryGetValue(inoutCon, out int _)) continue; //visited

					_conToNets.TryAdd(inoutCon, _networkCount);
					_bfsOpen.Enqueue(inoutCon);
					network.Add(inoutCon);
				} while (cons.TryGetNextValue(out inoutCon, ref it));
			}
		}
	}
}
using Model.Components;
using Model.Systems.States;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
		private int _networkCount;
		private EntityArchetype _networkArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(Network)),
				new ComponentType(typeof(NetAdjust)));
			World.GetOrCreateSystem<PathCacheCommandBufferSystem>();
			_outCons = new NativeMultiHashMap<Entity, Entity>(SystemConstants.MapNodeSize, Allocator.Persistent);
			_inCons = new NativeMultiHashMap<Entity, Entity>(SystemConstants.MapNodeSize, Allocator.Persistent);
			_conToNets = new NativeHashMap<Entity, int>(SystemConstants.MapConnectionSize, Allocator.Persistent);
			_newCons = new NativeQueue<Entity>(Allocator.Persistent);
			_bfsOpen = new NativeQueue<Entity>(Allocator.Persistent);
			_network = new NativeList<Entity>(SystemConstants.MapConnectionSize, Allocator.Persistent);
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
			
			//a node connection two connection of different levels is an exit node
			
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
					
					_conToNets.TryAdd(newCon, _networkCount);
					_bfsOpen.Enqueue(newCon);
					_network.Add(newCon);
					
					while (_bfsOpen.Count > 0)
					{
						var curConEnt = _bfsOpen.Dequeue();
						var connection = EntityManager.GetComponentData<Connection>(curConEnt);
						BFS(ref _outCons, ref _network, connection.EndNode, EntityManager, level);
						BFS(ref _inCons, ref _network, connection.StartNode, EntityManager, level);
					}

					var networkEnt = EntityManager.CreateEntity(_networkArchetype);
					EntityManager.SetComponentData(networkEnt, new Network
					{
						Index = _networkCount,
					});
					var buffer = EntityManager.GetBuffer<NetAdjust>(networkEnt);
					for (int i = 0; i < _network.Length; i++)
					{
						var conEnt = _network[i];
						var connection = EntityManager.GetComponentData<Connection>(conEnt);
						var conLen = EntityManager.GetComponentData<ConnectionLength>(conEnt);
						buffer.Add(new NetAdjust
						{
							Connection = conEnt,
							Cost = conLen.Length / connection.Speed,
							StartNode = connection.StartNode,
							EndNode = connection.EndNode,
						});
					}

					for (int i = 0; i < _network.Length; i++)
					{
						var conEnt = _network[i];
						EntityManager.AddSharedComponentData(conEnt, new NetworkGroup
						{
							NetworkId = networkEnt.Index,
						});
					}
				}
			}
			
			return inout;
		}

		private void BFS(ref NativeMultiHashMap<Entity, Entity> cons,
			ref NativeList<Entity> network, Entity curNode,
			EntityManager entityManager, int level)
		{
			if (cons.TryGetFirstValue(curNode, out var outCon, out var it))
			{
				do
				{
					if (_conToNets.TryGetValue(outCon, out int _)) continue;
					if (entityManager.GetComponentData<Connection>(outCon).Level != level) continue;
					_conToNets.TryAdd(outCon, _networkCount);
					_bfsOpen.Enqueue(outCon);
					network.Add(outCon);
				} while (cons.TryGetNextValue(out outCon, ref it));
			}
		}
	}
}
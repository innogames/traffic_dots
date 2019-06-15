using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems.City
{
	public struct Node : IComponentData
	{
		public float3 Position;
		public int Level;
	}

	public struct Connection : IComponentData
	{
		public Entity StartNode;
		public Entity EndNode;
		public float Cost;
		public int Level;
	}

	public struct NetworkData : ISharedComponentData, IEquatable<NetworkData>
	{
		public NativeHashMap<Entity, int> NodeToIndex;
		public NativeArray<float> Dist;
		public NativeArray<int> Next;
		public NativeHashMap<int, Entity> CoordToConnection;

		private const int Size = 20;
		private const int SizeSqr = Size * Size;
		private const Allocator Allo = Allocator.Persistent;

		public float Distance(Entity startNode, Entity endNode)
		{
			int startIdx = NodeToIndex[startNode];
			int endIdx = NodeToIndex[endNode];
			return Dist[ComputeCoord(startIdx, endIdx)];
		}

		public Entity NextConnection(Entity currentNode, Entity destinationNode)
		{
			int curIdx = NodeToIndex[currentNode];
			int endIdx = NodeToIndex[destinationNode];
			int nextNodeIdx = Next[ComputeCoord(curIdx, endIdx)];
			return CoordToConnection[ComputeCoord(curIdx, nextNodeIdx)];
		}

		private int ComputeCoord(int x, int y)
		{
			return x * Size + y;
		}

		public void AddNode(Entity node)
		{
			int len = NodeToIndex.Length;
			NodeToIndex.TryAdd(node, len);
			for (int i = 0; i < len; i++)
			{
				int iToNode = ComputeCoord(i, len);
				Dist[iToNode] = float.MaxValue;
				Next[iToNode] = -1;
				int nodeToI = ComputeCoord(len, i);
				Dist[nodeToI] = float.MaxValue;
				Next[nodeToI] = -1;
			}
			Dist[ComputeCoord(len, len)] = 0;
		}

		public void AddConnection(Entity entity, Connection connection)
		{
			//TODO optimize this
			
			int startIdx = NodeToIndex[connection.StartNode];
			int endIdx = NodeToIndex[connection.EndNode];
			CoordToConnection.TryAdd(ComputeCoord(startIdx, endIdx), entity);
			CoordToConnection.TryAdd(ComputeCoord(endIdx, startIdx), entity);	

			Dist[ComputeCoord(startIdx, endIdx)] = connection.Cost;
			Dist[ComputeCoord(endIdx, startIdx)] = connection.Cost;

			int len = NodeToIndex.Length;
			
			for (int k = 0; k < len; k++)
			{
				for (int i = 0; i < len; i++)
				{
					for (int j = 0; j < len; j++)
					{
						int ij = ComputeCoord(i, j);
						int ik = ComputeCoord(i, k);
						int kj = ComputeCoord(k, j);

						if (Dist[ij] > Dist[ik] + Dist[kj])
						{
							Dist[ij] = Dist[ik] + Dist[kj];
							Next[ij] = k;
						}
					}
				}
			}
		}

		public static NetworkData Create()
		{
			return new NetworkData
			{
				Dist = new NativeArray<float>(SizeSqr, Allo),
				Next = new NativeArray<int>(SizeSqr, Allo),
				NodeToIndex = new NativeHashMap<Entity, int>(Size, Allo),
				CoordToConnection = new NativeHashMap<int, Entity>(Size, Allo)
			};
		}

		public bool Equals(NetworkData other)
		{
			return NodeToIndex.Equals(other.NodeToIndex);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is NetworkData other && Equals(other);
		}

		public override int GetHashCode()
		{
			return NodeToIndex.GetHashCode();
		}
	}

	public struct NodeData : ISystemStateComponentData
	{
		public Entity ClosestExit;
		public Entity Network;
		public int IndexInNetwork;
	}

	public struct ConnectionData : ISystemStateComponentData
	{
		//TODO add traffic information here
	}
	
	///approach 3: City System: change to alley, road, highway
	///algorithm: find the closest exit to the higher network
	///e.g: from the alley, find the closest road, then the closest highway to the destination!
	///e.g: from the house, walk to the bus station, then to the train station, and so on!
	///cached info:
	///within the network: dist and next of all nodes
	///for each node: exit to the closest higher network node
	///e.g: house to intersection (how to prevent wrong one?), alley to road, road to highway
	
	///network separation?
	///if the shortest path between two nodes have to go through a higher node ==> they belong to two networks!
	///==> use coloring algorithm!
	///how to know before adding to a network? just append to an existing network, compute, then split!
	/// the new node is appended at the end, easy to split!
	///
	/// prevent wrong intersection from house: not allowing u-turn!
	/// so there is only one valid intersection from and to a house!
	/// for walking: can test 2x2 combination of start and end intersection!
	///
	/// NetworkData should be a SharedComponent on each node: to use shared component filter & cache locality!
	/// NetworkData could just have an ID for filter purpose!
	/// Dist and Next should be DynamicBuffer on each Node
	///
	/// Highway node should not belong to road network
	/// Road exit to high-way will first exit to the road node connected to highway (outer-node)
	/// These outer-node has exit directly to high-way node
	///
	/// update Dist and Next
	/// a new connection is created
	/// retrieve the network
	/// how to access dist[i][j]?
	/// NetworkData should link to another entity, for NetworkInternalData
	/// from Entity to index: indexInNetwork
	/// from index to Entity?
	///
	/// use Entity as index
	/// HashMap<Entity, float> dist; HashMap<Entity, Entity> next; per entity! NOT POSSIBLE!
	public class CitySystem : ComponentSystem
	{
		private EntityArchetype _networkArchetype;
		
		private void AddNode(Entity entity, ref Node node)
		{
			PostUpdateCommands.AddComponent(entity, new NodeData
			{
				ClosestExit = Entity.Null,
				Network = Entity.Null,
				IndexInNetwork = -1,
			});
		}

		private void AddNodeNow(Entity entity, ref Node node)
		{
			EntityManager.AddComponentData(entity, new NodeData
			{
				ClosestExit = Entity.Null,
				Network = Entity.Null,
				IndexInNetwork = -1,
			});
		}

		private void AddConnection(Entity entity, ref Connection connection, 
			ComponentDataFromEntity<NodeData> nodesData)
		{
			var startNode = nodesData[connection.StartNode];
			var endNode = nodesData[connection.EndNode];
			PostUpdateCommands.AddComponent(entity, new ConnectionData
			{
			});

			//the first network!
			if (startNode.Network == Entity.Null && endNode.Network == Entity.Null)
			{
				var network = PostUpdateCommands.CreateEntity(_networkArchetype);
				
				var networkData = NetworkData.Create();
				networkData.AddNode(connection.StartNode);
				networkData.AddNode(connection.EndNode);
				networkData.AddConnection(entity, connection);
				
				PostUpdateCommands.SetSharedComponent(network, networkData);

				startNode.Network = network;
				startNode.IndexInNetwork = 0;
				
				endNode.Network = network;
				endNode.IndexInNetwork = 1;
				
				PostUpdateCommands.SetComponent(connection.StartNode, startNode);
				PostUpdateCommands.SetComponent(connection.EndNode, endNode);
			}
			else if (endNode.Network == Entity.Null)//assume that isolated node is always endNode
			{
				endNode.Network = startNode.Network;
				var network = EntityManager.GetSharedComponentData<NetworkData>(startNode.Network);
				network.AddNode(connection.EndNode);
				network.AddConnection(entity, connection);
				
				PostUpdateCommands.SetComponent(connection.EndNode, endNode);
				PostUpdateCommands.SetSharedComponent(startNode.Network, network);
			}
			
			//two nodes same level
			//  two nodes same network
			//  different network

			//different level
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(NetworkData)));
		}

		protected override void OnUpdate()
		{
			Entities.WithNone<NodeData>().ForEach((Entity entity, ref Node node) =>
			{
				AddNode(entity, ref node);
			});
			var nodesData = GetComponentDataFromEntity<NodeData>();
			Entities.WithNone<ConnectionData>().ForEach((Entity entity, ref Connection connection) =>
			{
				AddConnection(entity, ref connection, nodesData);
			});
		}
	}
}
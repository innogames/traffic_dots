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

	public struct Agent : IComponentData
	{
		public float Speed;
	}

	public struct NodeAttachment : IComponentData
	{
		public Entity Node;
	}

	public struct PathIntent : IComponentData
	{
		public Entity EndNode;
	}

	public struct NodeData : ISystemStateComponentData
	{
		public Entity ClosestExit;
		public Entity Network;
		public int IndexInNetwork;
	}

	public struct NodeNetworkAssociation : ISharedComponentData
	{
		public Entity Network;
	}

	public struct NodeNetworkBuffer : IBufferElementData
	{
		public Entity NextHop;
	}

	public struct ConnectionData : ISystemStateComponentData
	{
		//TODO add traffic information here
	}

	public struct PathIntentData : ISystemStateComponentData
	{
		public Entity CurrentConnection;
		public float Lerp;
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
				networkData.UpdateNodeBuffer(EntityManager);
				
				PostUpdateCommands.SetSharedComponent(network, networkData);

				startNode.Network = network;
				startNode.IndexInNetwork = 0;
				
				endNode.Network = network;
				endNode.IndexInNetwork = 1;
				
				PostUpdateCommands.SetComponent(connection.StartNode, startNode);
				PostUpdateCommands.SetComponent(connection.EndNode, endNode);

#if USE_NETWORK_ASSOCIATION
				var networkAssociation = new NodeNetworkAssociation
				{
					Network = network,
				};
				PostUpdateCommands.AddSharedComponent(connection.StartNode, networkAssociation);
				PostUpdateCommands.AddSharedComponent(connection.EndNode, networkAssociation);
#endif
			}
			else if (endNode.Network == Entity.Null)//assume that isolated node is always endNode
			{
				endNode.Network = startNode.Network;
				//TODO rewrite without using EntityManager
				var networkData = EntityManager.GetSharedComponentData<NetworkData>(startNode.Network);
				networkData.AddNode(connection.EndNode);
				networkData.AddConnection(entity, connection);
				networkData.UpdateNodeBuffer(EntityManager);
				
				PostUpdateCommands.SetComponent(connection.EndNode, endNode);
				PostUpdateCommands.SetSharedComponent(startNode.Network, networkData);
			}
			
			//two nodes same level
			//  two nodes same network
			//  different network

			//different level
		}

		private void AddPathIntent(Entity entity, ref NodeAttachment node, ref PathIntent pathIntent,
			ComponentDataFromEntity<NodeData> nodesData)
		{
			//check network
			var startNodeData = nodesData[node.Node];
			var endNodeData = nodesData[pathIntent.EndNode];

			//same network
			if (startNodeData.Network == endNodeData.Network)
			{
				var network = EntityManager.GetSharedComponentData<NetworkData>(startNodeData.Network);
			
				PostUpdateCommands.AddComponent(entity, new PathIntentData
				{
					CurrentConnection = network.NextConnection(node.Node, pathIntent.EndNode),
					Lerp = 0f,
				});
			}
			
			//TODO different networks
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
			
			var nodesData = GetComponentDataFromEntity<NodeData>(true);
			
			Entities.WithNone<ConnectionData>().ForEach((Entity entity, ref Connection connection) =>
			{
				AddConnection(entity, ref connection, nodesData);
			});
			
			Entities.WithAll<Agent>().WithNone<PathIntentData>()
				.ForEach((Entity entity, ref NodeAttachment node, ref PathIntent pathIntent) =>
			{
				AddPathIntent(entity, ref node, ref pathIntent, nodesData);
			});
		}
	}

	/// <summary>
	///	a connection:
	/// - transform entrance
	/// - transform exit
	/// - visualize: spline interpolation for the car
	/// 
	/// road segment: is one connection
	///
	/// lane merge: is two connections from two lanes to one
	///
	/// lane split: is two connections from one lane to two: path finding will decide
	///
	/// four-way intersection: each lane has three connections to three lanes on other roads
	///
	/// full traffic:
	/// - a connection has a capacity on how many car
	/// - when full, the connection is blocked
	/// - it's a queue? implemented with buffer
	/// - whoever arrive register with the segment to enter
	/// - the segment will pull in car from the queue when it becomes empty!
	/// </summary>
	public class RoadVisualizerSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
		}
	}
}
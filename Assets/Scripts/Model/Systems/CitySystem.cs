using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
	}

	public struct NetCount : ISystemStateComponentData
	{
		public int Count;
	}

	public struct NetAdjust : IBufferElementData
	{
		public Entity Connection;
		public Entity StartNode;
		public Entity EndNode;
		public float Cost;
	}

	public struct NodeNetworkBuffer : IBufferElementData
	{
		public Entity NextHop;
	}

	public struct ConnectionData : ISystemStateComponentData
	{
		//TODO add traffic information here
	}
	
	public struct ConnectionColor : ISystemStateComponentData
	{
		//TODO add traffic information here
		public int NetworkId;
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
	[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
	public class CitySystem : JobComponentSystem
	{
		private EntityArchetype _networkArchetype;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			_endFrameBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(NetworkSharedData)),
				new ComponentType(typeof(NetCount)));
		}

//		[BurstCompile]
		[ExcludeComponent(typeof(NodeData))]
		private struct AddNodeJob : IJobForEachWithEntity<Node>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public EntityArchetype NetworkEntity;
			public void Execute(Entity entity, int index, [ReadOnly] ref Node node)
			{
				var network = CommandBuffer.CreateEntity(index, NetworkEntity);
				CommandBuffer.SetComponent(index, network, new NetCount {Count = 1});
				CommandBuffer.AddComponent(index, entity, new NodeData
				{
					ClosestExit = Entity.Null,
					Network = network,
				});
			}
		}

		//[BurstCompile]
		[ExcludeComponent(typeof(ConnectionData))]
		private struct AddConnectionJob : IJobForEachWithEntity<Connection>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			[ReadOnly] public BufferFromEntity<NetAdjust> Buffers;
			[ReadOnly] public ComponentDataFromEntity<NodeData> NodesData;
			[ReadOnly] public ComponentDataFromEntity<NetCount> NetCounts;
			public void Execute(Entity entity, int index, [ReadOnly] ref Connection connection)
			{
				var startNode = NodesData[connection.StartNode];
				var endNode = NodesData[connection.EndNode];
				CommandBuffer.AddComponent(index, entity, new ConnectionData
				{
				});

				if (startNode.Network == endNode.Network)
				{
					DynamicBuffer<NetAdjust> netAdjust;
					netAdjust = Buffers.Exists(startNode.Network)
						? Buffers[startNode.Network]
						: CommandBuffer.AddBuffer<NetAdjust>(index, startNode.Network);
					netAdjust.Add(new NetAdjust
					{
						Connection = entity,
						Cost = connection.Cost,
						StartNode = connection.StartNode,
						EndNode = connection.EndNode,
					});
				}
				else
				{
					var startCount = NetCounts[startNode.Network];
					var endCount = NetCounts[endNode.Network];
					CommandBuffer.SetComponent(index, startNode.Network, new NetCount
					{
						Count = startCount.Count + endCount.Count,
					});
					CommandBuffer.SetComponent(index, endNode.Network, new NetCount
					{
						Count = 0,
					});
					endNode.Network = startNode.Network;
					CommandBuffer.SetComponent(index, connection.EndNode, endNode);
				}
			}
		}
		
		private EndSimulationEntityCommandBufferSystem _endFrameBarrier;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var commandBuffer = _endFrameBarrier.CreateCommandBuffer().ToConcurrent();
			var addNodeJob = new AddNodeJob
			{
				CommandBuffer = commandBuffer,
				NetworkEntity = _networkArchetype,
			}.Schedule(this, inputDeps);
			
			var connectionJob = new AddConnectionJob
			{
				Buffers = GetBufferFromEntity<NetAdjust>(),
				NodesData = GetComponentDataFromEntity<NodeData>(),
				NetCounts = GetComponentDataFromEntity<NetCount>(),
				CommandBuffer = commandBuffer,
			}.Schedule(this, addNodeJob);
			
			return connectionJob;

			//NetworkData
			//NativeHashMap<EntityPair, Entity> Next
			//key: from & to Node
			//value: next connection!
			
			//add node:
			//+ Node
			//- NodeData
			//create NetworkSharedData entity: to store index mapping & count
			//add NodeData.Network to that new entity
			
			//add connection:
			//if in the same network: add connection to NetAdjust
			//if dif network: merge network, change NetCount
			
			//query all networkData with changed NetCount
			//delete all with NetCount == 0
			
			//all Network with NetAdjust
			//recompute Dist & Next
			//remove NetAdjust
			
			//(agent) PathIntent
			//if same network: create SameNetwork (shared)
			//if dif network: create HigherNetwork & LowerNetwork
			
			//in order to use Next, NodeToIndex must be presented! Must group Agent by Network!
			
			//SameNetwork: group by SameNetwork
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
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RoadVisualizerSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
		}
	}
}
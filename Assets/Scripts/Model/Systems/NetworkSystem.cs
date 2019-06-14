using Libs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Model.Systems
{
	public struct Node : IComponentData
	{
		public float3 Position;
	}

	/// <summary>
	/// represent the direct connection between two nodes
	/// </summary>
	public struct Connection : IComponentData
	{
		public Entity StartNode;
		public Entity EndNode;
		public float Cost;
	}

	public struct Network : IComponentData
	{
	}

	public struct Agent : IComponentData
	{
		public float Speed;
	}

	[InternalBufferCapacity(10)]
	public struct Path : IBufferElementData
	{
		public Entity Milestone;
		public int NetworkLvl;
	}

	public struct AgentAttachment : IComponentData
	{
		public Entity Connection;
		public float3 StartPosition;
		public float3 EndPosition;
		public float CostDistance;
		public long StartTimestamp;
	}

	public struct NodeAttachment : IComponentData
	{
		public Entity Node;
	}

	public class NetworkSystem : ComponentSystem
	{
		private struct NodeData
		{
			/// <summary>
			/// array of networks
			/// index 0 is the top network, which encompass ALL nodes
			/// last network is the smallest network that the node belongs to
			/// this network array form the node's address!
			/// </summary>
			public NativeList<int> Networks;
		}

		private NativeHashMap<Entity, NodeData> _nodeData;

		/// <summary>
		/// a network is a collection of connected nodes
		/// </summary>
		public struct NetworkData
		{
			public int Depth;
			public NativeHashMap<Entity, int> NodeToIndex;
			public NativeList<float> Dist;
			public NativeList<int> Next;
			public NativeHashMap<int, Entity> CoordToConnection;

			/// <summary>
			/// nodes that are in both current network and upper network
			/// </summary>
			public NativeList<Entity> UpperNodes;
			public NativeList<Entity> AllNodes;

			public int UpperNetworkIdx;

			private int _size;

			public float Distance(Entity startNode, Entity endNode)
			{
				int startIdx = NodeToIndex[startNode];
				int endIdx = NodeToIndex[endNode];
				return Dist[CoordToIndex(startIdx, endIdx)];
			}

			public Entity NextConnection(Entity currentNode, Entity destinationNode)
			{
				int curIdx = NodeToIndex[currentNode];
				int endIdx = NodeToIndex[destinationNode];
				int nextNodeIdx = Next[CoordToIndex(curIdx, endIdx)];
				return CoordToConnection[CoordToIndex(curIdx, nextNodeIdx)];
			}

			private int CoordToIndex(int x, int y)
			{
				return x * _size + y;
			}

			public void AddNode(Entity node)
			{
				//TODO
			}

			public void AddConnection(Entity connection)
			{
				//TODO
			}
		}

		private NativeList<NetworkData> _networkData;

		private ComponentDataFromEntity<Node> _nodes;
		private ComponentDataFromEntity<Connection> _connections;
		private ComponentDataFromEntity<Agent> _agents;
		private ComponentDataFromEntity<AgentAttachment> _agentAttachments;
		private ComponentDataFromEntity<NodeAttachment> _nodeAttachments;

		public int GetNetworkData(float3 position)
		{
			//check if this position is inside another network region
			var networkData = new NetworkData();
			_networkData.Add(networkData);
			return _networkData.Length - 1;
		}

		public int AddNode(Entity node)
		{
			int networkDataIdx = GetNetworkData(_nodes[node].Position);
			var networks = new NativeList<int>();
			while (networkDataIdx != -1)
			{
				networks.Add(networkDataIdx);
				var network = _networkData[networkDataIdx];
				network.AddNode(node);
				networkDataIdx = network.UpperNetworkIdx;
			}
			_nodeData.TryAdd(node, new NodeData
			{
				Networks = networks,
			});
			return networkDataIdx;
		}

		public void AddConnection(Entity connectionEntity)
		{
			var connection = _connections[connectionEntity];
			NodeData temp;
			int startNetwork = -1;
			int endNetwork = -1;
			if (!_nodeData.TryGetValue(connection.StartNode, out temp))
			{
				startNetwork = AddNode(connection.StartNode);
			}

			if (!_nodeData.TryGetValue(connection.EndNode, out temp))
			{
				endNetwork = AddNode(connection.EndNode);
			}

			//1. if two node are in the same networks => network cache change
			if (startNetwork == endNetwork)
			{
				_networkData[startNetwork].AddConnection(connectionEntity);
			}

			//2. if two node are in different networks ==> upper network change
			//3. move up a network => repeat 2
		}

		public void SetPath(Entity agentEntity, Entity endNodeEntity)
		{
			var agent = _agents[agentEntity];
			AgentAttachment attachment;
			NodeAttachment nodeAttachment;

			var endNodeData = _nodeData[endNodeEntity];
			Entity startNode;

			if (_agentAttachments.Exists(agentEntity))
			{
				attachment = _agentAttachments[agentEntity];
			}
			else
			{
				nodeAttachment = _nodeAttachments[agentEntity];
				startNode = nodeAttachment.Node;
			}
		}

		private struct PathData
		{
			public Entity Node;
			public int NetworkLvl;
			public Entity PrevNode;
			public float Distance; //from start node
			public float Heuristic; //heuristic distance to end node
		}

		private void AddNode(Entity prevNode, Entity curNode, int networkLvl, float distance, Entity endNode)
		{
			float heuristic = math.lengthsq((_nodes[curNode].Position - _nodes[endNode].Position));
			var pathData = new PathData
			{
				Node = curNode,
				NetworkLvl = networkLvl,
				PrevNode = prevNode,
				Distance = distance,
			};
			_openList.Add(pathData, distance + heuristic);
			_closeList.TryAdd(curNode, pathData);
		}

		private enum SearchDirection
		{
			UpStream,
			Plateau,
			DownStream,
		}

		public void ComputePath(Entity startNode, Entity endNode, Entity agentEntity)
		{
//			var agent = _agents[agentEntity];
			var startNodeData = _nodeData[startNode];
			var endNodeData = _nodeData[endNode];

			int startNetworkLvl = startNodeData.Networks.Length - 1;
			int startNetworkId = startNodeData.Networks[startNetworkLvl];

			int endNetworkLvl = endNodeData.Networks.Length - 1;
			int endNetworkId = endNodeData.Networks[endNetworkLvl];

			//TODO move this allocation to init
			_openList.Clear();
			_closeList.Clear();

			int curNetworkLvl = startNetworkLvl;

			AddNode(Entity.Null, startNode, curNetworkLvl, 0, endNode);

			while (_openList.Count > 0)
			{
				var min = _openList.ExtractMin(); //pop out as a binary heap

				if (min.Node == endNode)
				{
					//approach 1: just set the next target & recompute the path at the next miltstone
					//approach 2: get all the next "milestone" saved on the agent entity
					//- this can be jobified
					//- number of milestones are equal to number of network level * 2, which is low!
					//- could be generalized for target at each network level, needed during path travel as well!
					//approach 3: save all the milestone next pointers on each milestone entity
					//approach 4: save the next pointer on the networkData: may not be parallel compatible
					
					//approach 2 is the best
					var buffer = EntityManager.GetBuffer<Path>(agentEntity);
					var curNode = min.Node;
					while (curNode != startNode)
					{
						var pathData = _closeList[curNode];
						buffer.Add(new Path
						{
							Milestone = curNode,
							NetworkLvl = pathData.NetworkLvl,
						});
						curNode = pathData.PrevNode;
					}
				}

				PathData temp;
				if (_closeList.TryGetValue(min.Node, out temp))
				{
					continue;
				}

				var minData = _nodeData[min.Node];
				//TODO compute this value
				var searchDirection = SearchDirection.UpStream;
				
				//determine upstream or downstream
				var networkData = _networkData[minData.Networks[curNetworkLvl]];
				NativeList<Entity> neighbours;
				switch (searchDirection)
				{
					case SearchDirection.UpStream:
						neighbours = networkData.UpperNodes;
						curNetworkLvl--;
						break;
					case SearchDirection.Plateau:
						neighbours = networkData.AllNodes;
						break;
					case SearchDirection.DownStream:
						curNetworkLvl++;
						neighbours = _networkData[minData.Networks[curNetworkLvl]].AllNodes;
						break;
					default:
						neighbours = new NativeList<Entity>(); //this should not happen
						break;
				}
				//iterate through neighbours, compute G + H, add them to openList & closeList
				for (int i = 0; i < neighbours.Length; i++)
				{
					var neighbour = neighbours[i];
					float distance = min.Distance + networkData.Distance(min.Node, neighbour);
					AddNode(min.Node, neighbour, curNetworkLvl, distance, endNode);
				}
			}
		}

		private BinaryHeap<PathData, float> _openList;
		private NativeHashMap<Entity, PathData> _closeList;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			_openList = new BinaryHeap<PathData, float>(100, Allocator.Persistent);
			_closeList = new NativeHashMap<Entity, PathData>();
		}

		public Entity FollowPath(Entity agentEntity)
		{
			var agentNode = _nodeData[agentEntity];
			var agentNodeLvl = agentNode.Networks.Length - 1;
			
			var path = EntityManager.GetBuffer<Path>(agentEntity);
			var nextMilestone = path[path.Length - 1];
			while (nextMilestone.NetworkLvl < agentNodeLvl)
			{
				
			}

			return nextMilestone.Milestone;
			//milestone always share a network with current node
			//if milestone network level is the lowest (or 1 level above), use NetworkData to get next node
			//if milestone network level is higher, go to that network level to find next node
			//replace milestone with that next node, repeat
		}

		protected override void OnUpdate()
		{
		}
	}

	public class AgentVisualizer : ComponentSystem
	{
		protected override void OnUpdate()
		{
			long curTimestamp = 0; //TODO get the current timestamp here
			Entities.ForEach((ref Agent agent, ref AgentAttachment attachment, ref Translation translation) =>
			{
				float t = (curTimestamp - attachment.StartTimestamp) * agent.Speed / attachment.CostDistance;
				translation.Value = math.lerp(attachment.StartPosition, attachment.EndPosition, t);
			});
		}
	}
}
using System;
using Unity.Collections;
using Unity.Entities;

namespace Model.Systems.City
{
	public struct NetworkData : ISharedComponentData, IEquatable<NetworkData>
	{
		public NativeHashMap<Entity, int> NodeToIndex;
		public NativeList<Entity> Nodes;
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
			Nodes.Add(node);
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
			int startIdx = NodeToIndex[connection.StartNode];
			int endIdx = NodeToIndex[connection.EndNode];
			CoordToConnection.TryAdd(ComputeCoord(startIdx, endIdx), entity);
			CoordToConnection.TryAdd(ComputeCoord(endIdx, startIdx), entity);	

			Dist[ComputeCoord(startIdx, endIdx)] = connection.Cost;
			Dist[ComputeCoord(endIdx, startIdx)] = connection.Cost;

			int len = NodeToIndex.Length;
			
			//TODO optimize this
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

		public void UpdateNodeBuffer(EntityManager entityManager)
		{
			
			int len = NodeToIndex.Length;
			for (int i = 0; i < len; i++)
			{
				var node = Nodes[i];
				//parallel versions: use BufferFromEntity<NodeNetworkBuffer> buffers
				//var buffer = buffers[node];
				var buffer = entityManager.GetBuffer<NodeNetworkBuffer>(node);
				buffer.Clear();
				for (int j = 0; j < len; j++)
				{
					int ij = ComputeCoord(i, j);
					var nextHop = Nodes[Next[ij]];
					buffer.Add(new NodeNetworkBuffer
					{
						NextHop = nextHop,
					});
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
				Nodes = new NativeList<Entity>(Size, Allo),
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
}
using System;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;

namespace Model.Systems
{
	public struct NetworkCache : IEquatable<NetworkCache>, IDisposable
	{
		private struct Path : IEquatable<Path>
		{
			public readonly Entity From;
			public readonly Entity To;

			public Path(Entity from, Entity to)
			{
				From = from;
				To = to;
			}

			public bool Equals(Path other)
			{
				return From.Equals(other.From) && To.Equals(other.To);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is Path other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (From.GetHashCode() * 397) ^ To.GetHashCode();
				}
			}
		}

		private int _networkEntityIndex;
		private NativeHashMap<Path, Entity> _next;
		private NativeHashMap<Path, float> _dist;
		private NativeHashMap<Entity, int> _nodes;
		private NativeList<Entity> _cons;
		private NativeList<Path> _conInfos;

		public static NetworkCache Create(Entity networkEntity)
		{
			return new NetworkCache
			{
				_networkEntityIndex = networkEntity.Index,
				_next = new NativeHashMap<Path, Entity>(SystemConstants.NetworkNodeSqrSize, Allocator.Temp),
				_dist = new NativeHashMap<Path, float>(SystemConstants.NetworkNodeSqrSize, Allocator.Temp),
				_nodes = new NativeHashMap<Entity, int>(SystemConstants.NetworkNodeSize, Allocator.Temp),
				_cons = new NativeList<Entity>(SystemConstants.NetworkConnectionSize, Allocator.Temp),
				_conInfos = new NativeList<Path>(SystemConstants.NetworkConnectionSize, Allocator.Temp),
			};
		}

		public void AddConnection(Entity from, Entity to, float cost, Entity conEnt, Entity onlyNext)
		{
			_nodes.TryAdd(from, _nodes.Length);
			_nodes.TryAdd(to, _nodes.Length);
			var fromTo = new Path(from, to);
			if (onlyNext == Entity.Null)
			{
				_cons.Add(conEnt);
				_conInfos.Add(fromTo);
			}

			_dist.TryAdd(fromTo, cost);
			WriteNext(fromTo, conEnt);
		}

		private float ReadDist(Path path)
		{
			if (_dist.TryGetValue(path, out float result)) return result;

			return float.MaxValue;
		}

		private void WriteDist(Path path, float newVal)
		{
			if (_dist.TryGetValue(path, out float result)) _dist.Remove(path);

			_dist.TryAdd(path, newVal);
		}

		private void WriteNext(Path path, Entity connection)
		{
			if (_next.TryGetValue(path, out var result)) _next.Remove(path);

			_next.TryAdd(path, connection);
		}

		public int ConnectionCount()
		{
			return _cons.Length;
		}

		public void Compute2(EntityManager entityManager, ref NativeArray<Entity> entrances,
			ref NativeArray<Entity> exits)
		{
			int len = _nodes.Length;
			var nodes = _nodes.GetKeyArray(Allocator.Temp);

			for (int k = 0; k < len; k++)
			{
				var nodeK = nodes[k];
				var kk = new Path(nodeK, nodeK);
				WriteDist(kk, 0);
			}

			for (int k = 0; k < len; k++)
			{
				var nodeK = nodes[k];
				for (int i = 0; i < len; i++)
				{
					var nodeI = nodes[i];
					for (int j = 0; j < len; j++)
					{
						var nodeJ = nodes[j];
						var ij = new Path(nodeI, nodeJ);
						var ik = new Path(nodeI, nodeK);
						float newDist = ReadDist(ik) + ReadDist(new Path(nodeK, nodeJ));

						if (ReadDist(ij) > newDist)
						{
							WriteDist(ij, newDist);
							WriteNext(ij, _next[ik]);
						}
					}
				}
			}

			int conLen = _cons.Length;
			for (int i = 0; i < conLen; i++)
			{
				var conI = _cons[i];
				entityManager.AddComponentData(conI, new IndexInNetwork {Index = i});
				var endI = _conInfos[i].To;
				var buffer = entityManager.AddBuffer<NextBuffer>(conI);
				ComputeNextBuffer(ref exits, conLen, i, ref endI, ref buffer);
			}

			for (int i = 0; i < entrances.Length; i++)
			{
				var entrance = entrances[i];
				var buffer = entityManager.AddBuffer<NextBuffer>(entrance);
				ComputeNextBuffer(ref exits, conLen, -1, ref entrance, ref buffer);
			}

			nodes.Dispose();
		}

		private void ComputeNextBuffer(ref NativeArray<Entity> exits, int conLen, int index, ref Entity startNode,
			ref DynamicBuffer<NextBuffer> buffer)
		{
			//con to con
			for (int j = 0; j < conLen; j++)
			{
				Entity nextCon;
				var conJ = _cons[j];
				var endNode = _conInfos[j].From;
				if (index == j)
				{
					nextCon = Entity.Null;
				}
				else if (startNode == endNode)
				{
					nextCon = conJ;
				}
				else
				{
					if (!_next.TryGetValue(new Path(startNode, endNode), out nextCon))
					{
						nextCon = Entity.Null;
					}
				}

				buffer.Add(new NextBuffer
				{
					Connection = nextCon,
				});
			}

			//con to exit
			for (int j = 0; j < exits.Length; j++)
			{
				var exit = exits[j];
				if (!_next.TryGetValue(new Path(startNode, exit), out var nextCon))
				{
					nextCon = Entity.Null;
				}

				buffer.Add(new NextBuffer
				{
					Connection = nextCon,
				});
			}
		}

		public void Compute(int index, EntityCommandBuffer.Concurrent commandBuffer)
		{
			int len = _nodes.Length;
			var nodes = _nodes.GetKeyArray(Allocator.Temp);

			for (int k = 0; k < len; k++)
			{
				var nodeK = nodes[k];
				var kk = new Path(nodeK, nodeK);
				WriteDist(kk, 0);
			}

			for (int k = 0; k < len; k++)
			{
				var nodeK = nodes[k];
				for (int i = 0; i < len; i++)
				{
					var nodeI = nodes[i];
					for (int j = 0; j < len; j++)
					{
						var nodeJ = nodes[j];
						var ij = new Path(nodeI, nodeJ);
						var ik = new Path(nodeI, nodeK);
						float newDist = ReadDist(ik) + ReadDist(new Path(nodeK, nodeJ));

						if (ReadDist(ij) > newDist)
						{
							WriteDist(ij, newDist);
							WriteNext(ij, _next[ik]);
						}
					}
				}
			}

			int conLen = _cons.Length;
			for (int i = 0; i < conLen; i++)
			{
				var conEnt = _cons[i];
				commandBuffer.AddComponent(index, conEnt, new IndexInNetwork {Index = i});
			}

			for (int i = 0; i < conLen; i++)
			{
				var conI = _cons[i];
				var endI = _conInfos[i].To;
				var buffer = commandBuffer.AddBuffer<NextBuffer>(index, conI);
				for (int j = 0; j < conLen; j++)
				{
					Entity nextCon;
					var conJ = _cons[j];
					var fromJ = _conInfos[j].From;
					if (i == j)
					{
						nextCon = Entity.Null;
					}
					else if (endI == fromJ)
					{
						nextCon = conJ;
					}
					else
					{
						if (!_next.TryGetValue(new Path(endI, fromJ), out nextCon))
						{
							nextCon = Entity.Null;
						}
					}

					buffer.Add(new NextBuffer
					{
						Connection = nextCon,
					});
				}
			}

			nodes.Dispose();
		}

		public void Dispose()
		{
			_next.Dispose();
			_dist.Dispose();
			_nodes.Dispose();
			_cons.Dispose();
			_conInfos.Dispose();
		}

		public bool Equals(NetworkCache other)
		{
			return _networkEntityIndex.Equals(other._networkEntityIndex);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is NetworkCache other && Equals(other);
		}

		public override int GetHashCode()
		{
			return _networkEntityIndex.GetHashCode();
		}
	}
}
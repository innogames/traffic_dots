using System;
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

		public static NetworkCache Create(Entity networkEntity)
		{
			return new NetworkCache
			{
				_networkEntityIndex = networkEntity.Index,
				_next = new NativeHashMap<Path, Entity>(CityConstants.NetworkSizeSqr, Allocator.Temp),
				_dist = new NativeHashMap<Path, float>(CityConstants.NetworkSizeSqr, Allocator.Temp),
				_nodes = new NativeHashMap<Entity, int>(CityConstants.MapNodeSize, Allocator.Temp)
			};
		}

		public void AddConnection(Entity from, Entity to, float cost, Entity connection)
		{
			_nodes.TryAdd(from, _nodes.Length);
			_nodes.TryAdd(to, _nodes.Length);
			var fromTo = new Path(from, to);
			_dist.TryAdd(fromTo, cost);
			WriteNext(fromTo, connection);
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
				commandBuffer.AddComponent(index, nodeK, new IndexInNetwork {Index = k});
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

			for (int i = 0; i < len; i++)
			{
				var nodeI = nodes[i];
				var buffer = commandBuffer.SetBuffer<NextBuffer>(index, nodeI);
				for (int j = 0; j < len; j++)
				{
					var nodeJ = nodes[j];
					var ij = new Path(nodeI, nodeJ);

					buffer.Add(new NextBuffer
					{
						Connection = _next.TryGetValue(ij, out var connection)
							? connection
							: Entity.Null
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
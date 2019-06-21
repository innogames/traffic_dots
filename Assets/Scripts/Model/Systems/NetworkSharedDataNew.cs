using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems.City
{
	public struct NetworkSharedDataNew : ISharedComponentData, IEquatable<NetworkSharedDataNew>
	{
		public int NetworkEntityIndex;
		public NativeHashMap<Path, Entity> Next;
		public NativeHashMap<Path, Entity> Connections;
		public NativeHashMap<Path, float> Dist;
		public NativeHashMap<Entity, int> Nodes;

		public static NetworkSharedDataNew Create(Entity networkEntity)
		{
			return new NetworkSharedDataNew
			{
				NetworkEntityIndex = networkEntity.Index,
				Next = new NativeHashMap<Path, Entity>(CityConstants.NetworkSizeSqr, Allocator.Temp),
				Connections = new NativeHashMap<Path, Entity>(CityConstants.NetworkSizeSqr, Allocator.Temp),
				Dist = new NativeHashMap<Path, float>(CityConstants.NetworkSizeSqr, Allocator.Temp),
				Nodes = new NativeHashMap<Entity, int>(CityConstants.MapNodeSize, Allocator.Temp),
			};
		}

		public void AddConnection(Entity from, Entity to, float cost, Entity connection)
		{
			Nodes.TryAdd(from, Nodes.Length);
			Nodes.TryAdd(to, Nodes.Length);
			var fromTo = new Path(from, to);
			var toFrom = new Path(to, from);
			Dist.TryAdd(fromTo, cost);
			Dist.TryAdd(toFrom, cost);
			Connections.TryAdd(fromTo, connection);
			Connections.TryAdd(toFrom, connection);
		}

		private float ReadDist(Path path)
		{
			if (Dist.TryGetValue(path, out float result))
			{
				return result;
			}
			return float.MaxValue;
		}

		private void WriteDist(Path path, float newVal)
		{
			if (Dist.TryGetValue(path, out float result))
			{
				Dist.Remove(path);
			}

			Dist.TryAdd(path, newVal);
		}
		
		private void WriteNext(Path path, Entity connection)
		{
			if (Next.TryGetValue(path, out Entity result))
			{
				Next.Remove(path);
			}

			Next.TryAdd(path, connection);
		}

		public void Compute(int index, EntityCommandBuffer.Concurrent commandBuffer)
		{
			int len = Nodes.Length;
			var nodes = Nodes.GetKeyArray(Allocator.Temp);

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
						var ij = new Path(nodeI, nodeI);
						var ik = new Path(nodeI, nodeK);
						float newDist = ReadDist(ik) + ReadDist(new Path(nodeK, nodeJ));

						if (ReadDist(ij) > newDist)
						{
							WriteDist(ij, newDist);
							var connection = Connections[ik];
							WriteNext(ij, connection);
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
					var ij = new Path(nodeI, nodeI);

					buffer.Add(new NextBuffer
					{
						Connection = Next.TryGetValue(ij, out var connection)
							? connection
							: Entity.Null
					});
				}
			}
			
			nodes.Dispose();
			Next.Dispose();
			Dist.Dispose();
			Nodes.Dispose();
			Connections.Dispose();
		}

		public Entity NextConnection(Entity from, Entity to)
		{
			return Next[new Path(from, to)];
		}

		public bool Equals(NetworkSharedDataNew other)
		{
			return NetworkEntityIndex.Equals(other.NetworkEntityIndex);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is NetworkSharedDataNew other && Equals(other);
		}

		public override int GetHashCode()
		{
			return NetworkEntityIndex.GetHashCode();
		}
	}
}
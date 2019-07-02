using Model.Components;
using Model.Systems.States;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems
{
	[DisableAutoCreation]
	[UpdateInGroup(typeof(CitySystemGroup))]
	public class CityAddConnectionSeqSystem : ComponentSystem
	{
		private EntityArchetype _networkArchetype;
		private EntityQuery _query;

		protected override void OnCreate()
		{
			base.OnCreate();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(Network)),
				new ComponentType(typeof(NetAdjust)));
			_query = EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All = new[] {new ComponentType(typeof(Connection))},
				None = new[] {new ComponentType(typeof(NetworkGroup)),}
			});
		}

		protected override void OnUpdate()
		{
			var conToColor = new NativeHashMap<Entity, int>(SystemConstants.MapNodeSize, Allocator.Temp);
			var colorToColor = new NativeHashMap<int, int>(SystemConstants.MapNodeSize, Allocator.Temp);
			int newColor = 0;

			var connections = _query.ToComponentDataArray<Connection>(Allocator.TempJob);
			var conEnts = _query.ToEntityArray(Allocator.TempJob);

			for (int i = 0; i < connections.Length; i++)
			{
				var conA = connections[i];
				var entA = conEnts[i];
				for (int j = 0; j < connections.Length; j++)
				{
					var conB = connections[j];
					var entB = conEnts[j];
					
					if (!conToColor.TryGetValue(entA, out int startColor)) startColor = int.MaxValue;
					;
					if (!conToColor.TryGetValue(entB, out int endColor)) endColor = int.MaxValue;

					if (startColor == endColor)
					{
						if (startColor == int.MaxValue)
						{
							conToColor.TryAdd(entA, newColor);
							conToColor.TryAdd(entB, newColor);
							newColor++;
						}
					}
					else
					{
						int minColor = math.min(startColor, endColor);
						int maxColor = math.max(startColor, endColor);
						var changedCon = startColor > endColor ? entA : entB;
						int trueColor = minColor;
						while (colorToColor.TryGetValue(trueColor, out int nextColor))
						{
							trueColor = nextColor;
						}

						if (maxColor < int.MaxValue) conToColor.Remove(changedCon);
						conToColor.TryAdd(changedCon, trueColor);

						if (maxColor < int.MaxValue)
						{
							if (colorToColor.TryGetValue(maxColor, out int temp)) colorToColor.Remove(maxColor);
							colorToColor.TryAdd(maxColor, trueColor);						
						}
					}
				}
			}
			
			connections.Dispose();
			conEnts.Dispose();

			if (conToColor.Length > 0)
			{
				var finalColor = new NativeHashMap<Entity, int>(SystemConstants.MapNodeSize, Allocator.Temp);				
				var colorToNetwork = new NativeHashMap<int, Entity>(SystemConstants.MapNodeSize, Allocator.Temp);
				var keys = conToColor.GetKeyArray(Allocator.Temp);
				var values = conToColor.GetValueArray(Allocator.Temp);

				for (int i = 0; i < keys.Length; i++)
				{
					var con = keys[i];
					int trueColor = values[i];
					while (colorToColor.TryGetValue(trueColor, out int nextColor))
					{
						trueColor = nextColor;
					}

					finalColor.TryAdd(con, trueColor);

					if (!colorToNetwork.TryGetValue(trueColor, out var network))
					{
						network = PostUpdateCommands.CreateEntity(_networkArchetype);
						colorToNetwork.TryAdd(trueColor, network);
					}
				}

				keys.Dispose();
				values.Dispose();

				var networkToBuffer =
					new NativeHashMap<Entity, DynamicBuffer<NetAdjust>>(SystemConstants.MapNodeSize, Allocator.Temp);

				Entities.WithNone<NetworkGroup>().ForEach((Entity connectionEnt, ref Connection connection,
					ref ConnectionLength conLength) =>
				{
					int color = finalColor[connectionEnt];
					var network = colorToNetwork[color];
					PostUpdateCommands.AddSharedComponent(connectionEnt, new NetworkGroup
					{
						NetworkId = network.Index
					});
					DynamicBuffer<NetAdjust> buffer;
					if (!networkToBuffer.TryGetValue(network, out buffer))
					{
						buffer = PostUpdateCommands.SetBuffer<NetAdjust>(network);
						networkToBuffer.TryAdd(network, buffer);
					}

					buffer.Add(new NetAdjust
					{
						Connection = connectionEnt,
						Cost = conLength.Length / connection.Speed,
						StartNode = connection.StartNode,
						EndNode = connection.EndNode
					});
				});
				colorToNetwork.Dispose();

				networkToBuffer.Dispose();
				finalColor.Dispose();
			}

			conToColor.Dispose();
			colorToColor.Dispose();
		}
	}
}
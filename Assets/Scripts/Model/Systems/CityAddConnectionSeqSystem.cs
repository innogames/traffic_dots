using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Model.Systems.City
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(NodeDataCommandBufferSystem))]
	[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
	public class CityAddConnectionSeqSystem : ComponentSystem
	{
		private EntityArchetype _networkArchetype;
		protected override void OnCreate()
		{
			base.OnCreate();
			_networkArchetype = EntityManager.CreateArchetype(new ComponentType(typeof(NetworkSharedData)));
		}

		protected override void OnUpdate()
		{
			var finalColor = new NativeHashMap<Entity, int>(CityConstants.MapNodeSize, Allocator.Temp);
			var nodeToColor = new NativeHashMap<Entity, int>(CityConstants.MapNodeSize, Allocator.Temp);
			var colorToColor = new NativeHashMap<int, int>(CityConstants.MapNodeSize, Allocator.Temp);
			int newColor = 0;
			Entities.WithNone<ConnectionData>().ForEach((ref Connection connection) =>
			{
				if (!nodeToColor.TryGetValue(connection.StartNode, out int startColor))
				{
					startColor = int.MaxValue;
				};
				if (!nodeToColor.TryGetValue(connection.EndNode, out int endColor))
				{
					endColor = int.MaxValue;
				}

				if (startColor == endColor)
				{
					if (startColor == int.MaxValue)
					{
						nodeToColor.TryAdd(connection.StartNode, newColor);
						nodeToColor.TryAdd(connection.EndNode, newColor);
						newColor++;
					}
				}
				else
				{
					int minColor = math.min(startColor, endColor);
					int maxColor = math.max(startColor, endColor);
					var changedNode = startColor < endColor ? connection.StartNode : connection.EndNode;
					int trueColor = minColor;
					while (colorToColor.TryGetValue(trueColor, out trueColor))
					{
					}

					if (maxColor < int.MaxValue)
					{
						nodeToColor.Remove(changedNode);
					}

					if (colorToColor.TryGetValue(maxColor, out int temp))
					{
						colorToColor.Remove(maxColor);
					}

					nodeToColor.TryAdd(changedNode, trueColor);
					colorToColor.TryAdd(maxColor, trueColor);
				}
			});

			if (nodeToColor.Length > 0)
			{
				var colorToNetwork = new NativeHashMap<int, Entity>(CityConstants.MapNodeSize, Allocator.Temp);
				var keys = nodeToColor.GetKeyArray(Allocator.Temp);
				var values = nodeToColor.GetValueArray(Allocator.Temp);

				for (int i = 0; i < keys.Length; i++)
				{
					var node = keys[i];
					int trueColor = values[i];
					while (colorToColor.TryGetValue(trueColor, out trueColor))
					{
					}

					finalColor.TryAdd(node, trueColor);

					if (!colorToNetwork.TryGetValue(trueColor, out var network))
					{
						network = PostUpdateCommands.CreateEntity(_networkArchetype);
						colorToNetwork.TryAdd(trueColor, network);
					}
				}
				
				keys.Dispose();
				values.Dispose();

				Entities.WithNone<ConnectionData>().ForEach((Entity connectionEnt, ref Connection connection) =>
				{
					int color = finalColor[connection.StartNode];
					var network = colorToNetwork[color];
					PostUpdateCommands.AddSharedComponent(connectionEnt, new ConnectionData
					{
						Network = network,
					});
				});
				colorToNetwork.Dispose();
			}
			
			finalColor.Dispose();
			nodeToColor.Dispose();
			colorToColor.Dispose();
		}
	}
}
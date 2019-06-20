using Unity.Entities;

namespace Model.Systems.City
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(CityAddConnectionSeqSystem))]
	[UpdateBefore(typeof(PathSystem))]
	public class NetworkCreationSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			
//			Entities.WithNone<NetworkSharedDataNew>().ForEach((Entity entity, ref Network network) =>
//			{
//				var networkShared = NetworkSharedDataNew.Create(entity);
//				
//				var adjusts = EntityManager.GetBuffer<NetAdjust>(entity);
//				for (int i = 0; i < adjusts.Length; i++)
//				{
//					var adjust = adjusts[i];
//					networkShared.AddConnection(adjust.StartNode, adjust.EndNode, adjust.Cost, adjust.Connection);
//				}
//				
//				networkShared.Compute();
//				PostUpdateCommands.AddSharedComponent(entity, networkShared);
//			});
		}
	}
}
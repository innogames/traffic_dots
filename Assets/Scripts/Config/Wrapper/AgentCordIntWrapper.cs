using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config.Wrapper
{
	[RequiresEntityConversion]
	public class AgentCordIntWrapper : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new AgentCordInt());
		}
	}
}
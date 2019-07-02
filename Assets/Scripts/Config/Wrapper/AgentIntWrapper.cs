using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config.Wrapper
{
	[RequiresEntityConversion]
	public class AgentIntWrapper : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float Length;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new AgentInt
			{
				Length = Length.ToCityInt(),
			});
		}
	}
}
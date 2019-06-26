using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config.Wrapper
{
	[RequiresEntityConversion]
	public class TailCoordWrapper : MonoBehaviour, IConvertGameObjectToEntity
	{
		//we don't need any fields here
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new TailCoord
			{
				Connection = Entity.Null,
			});
		}
	}
}
using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config.Wrapper
{
	[RequiresEntityConversion]
	public class ConnectionTargetWrapper : MonoBehaviour, IConvertGameObjectToEntity
	{
		//TODO add fields
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new ConnectionTarget());
			dstManager.AddComponentData(entity, new ConnectionTargetState
			{
				NextTarget = Entity.Null,
				TailTarget = Entity.Null,
			});
		}
	}
}
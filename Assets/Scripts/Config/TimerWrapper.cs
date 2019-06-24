using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	[RequiresEntityConversion]
	public class TimerWrapper : MonoBehaviour, IConvertGameObjectToEntity
	{
		public int Frames;
		public TimerType TimerType;
		//TODO add fields
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new Timer
			{
				Frames = Frames,
				TimerType = TimerType,
			});
		}
	}
}
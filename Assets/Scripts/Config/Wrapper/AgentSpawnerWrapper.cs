using System.Collections.Generic;
using System.Linq;
using Model.Components.Buffer;
using Unity.Entities;
using UnityEngine;

namespace Config.Wrapper
{
	[RequiresEntityConversion]
	public class AgentSpawnerWrapper : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
	{
		public GameObject[] Agents;
    
		// Lets you convert the editor data representation to the entity optimal runtime representation

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var spawnerData = new Model.Components.AgentSpawner
			{
				CurrentIndex = 0,
			};
			dstManager.AddComponentData(entity, spawnerData);
			var buffer = dstManager.AddBuffer<SpawnerBuffer>(entity);
			buffer.CopyFrom(
				Agents.Select(agent =>new SpawnerBuffer{Agent = conversionSystem.GetPrimaryEntity(agent)}).ToArray());
		}

		// Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
		public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
		{
			referencedPrefabs.AddRange(Agents);
		}
	}
}
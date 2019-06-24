using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	[RequiresEntityConversion]
	public class AgentSpawnerWrapper : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
	{
		public GameObject Agent;
    
		// Lets you convert the editor data representation to the entity optimal runtime representation

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var spawnerData = new Model.Components.AgentSpawner
			{
				
				// The referenced prefab will be converted due to DeclareReferencedPrefabs.
				// So here we simply map the game object to an entity reference to that prefab.
				Agent = conversionSystem.GetPrimaryEntity(Agent),
			};
			dstManager.AddSharedComponentData(entity, spawnerData);
		}

		// Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
		public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
		{
			referencedPrefabs.Add(Agent);
		}
	}
}
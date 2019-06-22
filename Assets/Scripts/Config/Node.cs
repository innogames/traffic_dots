using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Config
{
	public class Node : MonoBehaviour
	{
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(transform.position, 1.0f);
		}

		private void Awake()
		{
			gameObject.AddComponent<GameObjectEntity>();
			var node = gameObject.AddComponent<NodeProxy>();
			node.Value = new Model.Components.Node
			{
				Position = new float3(transform.position),
				Level = 0, //TODO
			};
		}
	}
}
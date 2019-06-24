using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Config
{
	public class Node : BaseGenerator
	{
		public Node NodePointer;

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(transform.position, 1.0f);
		}

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			if (this == NodePointer)
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
#endif
	}
}
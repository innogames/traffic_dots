using Config.Proxy;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;

namespace Config
{
	public class Node : BaseGenerator
	{
		public Node NodePointer;

#if UNITY_EDITOR
		public Node GenTimePointer => IsSharedNode() ? NodePointer : this;

		private readonly Vector3 size = new Vector3(1f, 1f, 2f);

		private void OnDrawGizmos()
		{
			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawMesh(GetConfig.ConeMesh, transform.position, transform.rotation,
					size);
			}
		}

		private bool IsSharedNode()
		{
			var connector = transform.parent.GetComponent<Connector>();
			return connector != null && !connector.IsDeadEnd();
		}

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			if (this == NodePointer || !IsSharedNode())
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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Config
{
	public class Connector : BaseGenerator
	{
		public Connector ConnectedTo;
		public int ConnectedToIndex = 0;
		[EnumFlags] public ConnectorType ConnectorType = ConnectorType.TwoLane;

		public IEnumerable<Node> SharedNodes => GetComponentsInChildren<Node>();

		private readonly Vector3 size = new Vector3(2f, 2f, 4f);
		
		#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (ConnectedTo == null)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawMesh(GetConfig.ConeMesh, transform.position, transform.rotation,
					size);
			}
		}
		#endif

		public void ConnectNodes()
		{
			foreach (var myNode in SharedNodes)
			{
//				if (myNode.NodePointer != null) continue; //this cause bugs if starts from old segment
				foreach (var other in ConnectedTo.SharedNodes)
				{
					if (ConfigConstants.Connected(myNode.transform, other.transform))
					{
						myNode.NodePointer = myNode;
						other.NodePointer = myNode;
					}
				}
			}
		}

		public bool IsDeadEnd()
		{
			return ConnectedTo == null ||
			       ConnectedTo.ConnectorType.Compatible(ConnectorType.Building | ConnectorType.BigBuilding);
		}
	}
}
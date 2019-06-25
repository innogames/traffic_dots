using System;
using System.Collections.Generic;
using UnityEngine;

namespace Config
{
	public class Connector : MonoBehaviour
	{
		public Connector ConnectedTo;
		public int ConnectedToIndex = 0;
		[EnumFlags] public ConnectorType ConnectorType = ConnectorType.TwoLane;

		public IEnumerable<Node> SharedNodes => GetComponentsInChildren<Node>();

		private void OnDrawGizmos()
		{
			if (ConnectedTo == null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(transform.position, 1f);
			}
		}

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
	}
}
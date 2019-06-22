using UnityEngine;

namespace Config
{
	public class Connector : MonoBehaviour
	{
		public Connector ConnectedTo;
		public int ConnectedToIndex = 0;
		public ConnectorType ConnectorType;
		public Node[] SharedNodes;

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
				if (myNode.NodePointer != null) continue;
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
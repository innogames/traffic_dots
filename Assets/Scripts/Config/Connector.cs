using UnityEngine;

namespace Config
{
	public class Connector : MonoBehaviour
	{
		public Connector ConnectedTo;
		public int ConnectedToIndex = 0;
		public ConnectorType ConnectorType;
		public Node[] SharedNodes;
	}
}
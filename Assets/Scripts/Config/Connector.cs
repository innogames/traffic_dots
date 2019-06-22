using UnityEngine;

namespace Config
{
	public class Connector : MonoBehaviour
	{
		public Connector ConnectedTo;
		public ConnectorType ConnectorType;
		public Node[] SharedNodes;
	}
}
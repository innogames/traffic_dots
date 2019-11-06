using UnityEditor;

namespace Config.CityEditor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Connector))]
	public class ConnectorEditor : SnapEditor<Connector>
	{
	}
}
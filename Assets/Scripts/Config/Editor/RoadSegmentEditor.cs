using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Config
{
	[CustomEditor(typeof(RoadSegment))]
	public class RoadSegmentEditor: Editor
	{
		private Connector _selectedConnector;
		private int _selectedIndex = -1;
		private void OnSceneGUI()
		{
			var segment = (RoadSegment)target;
			
			for (int i = 0; i < segment.Connectors.Length; i++)
			{
				var connector = segment.Connectors[i];
				Handles.color = connector == _selectedConnector ? Color.green : Color.white;
				if (Handles.Button(connector.transform.position, connector.transform.rotation, 3f, 3f, Handles.ConeHandleCap))
				{
					_selectedConnector = connector;
					_selectedIndex = i;
				}
			}
		}

		private const int IconSize = 50;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			RoadSegment segment = (RoadSegment)target;

			if (GUILayout.Button("Populate"))
			{
				segment.Connectors = segment.gameObject.GetComponentsInChildren<Connector>();
				if (segment.Config == null)
				{
					segment.Config = Resources.FindObjectsOfTypeAll<CityConfig>().First();
				}
				EditorUtility.SetDirty(segment);
			}
			
			EditorGUILayout.BeginVertical();
			if (_selectedConnector != null)
			{
				AssetPreview.SetPreviewTextureCacheSize(IconSize);

				var validSegments = segment.Config.Segments.Where(seg =>
					seg.Connectors.Any(con => con.ConnectorType == _selectedConnector.ConnectorType));
				foreach (var other in validSegments)
				{
					var texture = AssetPreview.GetAssetPreview(other.gameObject);
					if (GUILayout.Button(texture, GUILayout.Width(IconSize), GUILayout.Height(IconSize)))
					{
						int index = 0;
						if (_selectedConnector.ConnectedTo != null)
						{
							var exist = _selectedConnector.ConnectedTo.GetComponentInParent<RoadSegment>();
							if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(exist) == other)
							{
								index = (_selectedConnector.ConnectedToIndex + 1) % other.Connectors.Length;								
							}
							DestroyImmediate(exist.gameObject);
							_selectedConnector.ConnectedTo = null;
							_selectedConnector.ConnectedToIndex = 0;
						}

						for (; index < other.Connectors.Length; index++)
						{
							if (_selectedConnector.ConnectorType == other.Connectors[index].ConnectorType)
							{
								break;
							}
						}
						PlaceSegment(segment.transform, _selectedConnector, other, index);							
					}
				}

//				int selected = GUILayout.SelectionGrid(selected,
//					validSegments.Select(seg => AssetPreview.GetAssetPreview(seg.gameObject)).ToArray(), 5,
//					GUILayout.Width(IconSize), GUILayout.Height(IconSize));
			}
			EditorGUILayout.EndVertical();
		}
		
		public void PlaceSegment(Transform curSegment, Connector myConnector, RoadSegment otherSegment, int otherConnectorId)
		{			
			var newSegment = PrefabUtility.InstantiatePrefab(otherSegment, curSegment.parent) as RoadSegment;
			var otherConnector = newSegment.Connectors[otherConnectorId];
			var position = myConnector.transform.position;
			
			//myCon.rotation * 180 == newSeg.rotation * newCon.localRotation
			//newSeg.rotation = myCon.rotation * 180 / newCon.localRotation
			newSegment.transform.rotation = myConnector.transform.rotation * Quaternion.AngleAxis(180f, Vector3.up) *
			                                Quaternion.Inverse(otherConnector.transform.localRotation);

			//myCon.position = newCon.position = newSeg.position +  newSeg.rotation * newCon.localPosition
			//newSeg.position = position - newSeg.rotation * newCon.localPosition				                          
			newSegment.transform.position = position - newSegment.transform.rotation * otherConnector.transform.localPosition;

			myConnector.ConnectedTo = otherConnector;
			myConnector.ConnectedToIndex = otherConnectorId;
			otherConnector.ConnectedTo = myConnector;
			otherConnector.ConnectedToIndex = _selectedIndex;
			
			Undo.RecordObject(newSegment.gameObject, "Newly connect Segment");
		}

	}
}
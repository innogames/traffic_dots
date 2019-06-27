using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Config.CityEditor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(RoadSegment))]
	public class RoadSegmentEditor : Editor
	{
		private Connector _selectedConnector;
		private int _selectedIndex = -1;

		private void OnSceneGUI()
		{
			var segment = (RoadSegment) target;

			if (segment.Connectors != null)
			{
				for (int i = 0; i < segment.Connectors.Length; i++)
				{
					var connector = segment.Connectors[i];
					Handles.color = connector == _selectedConnector ? Color.green : Color.white;
					if (Handles.Button(connector.transform.position, connector.transform.rotation, 3f, 3f,
						Handles.ConeHandleCap))
					{
						_selectedConnector = connector;
						_selectedIndex = i;
					}
				}
			}

//			foreach (var connection in segment.GetComponentsInChildren<Connection>())
//			{
//				int index = 0;
//				foreach (var pos in connection.SlotSteps(connection.BezierFunc()))
//				{
//					if (Handles.Button(pos, Quaternion.LookRotation(Vector3.up), 
//						1, 1, Handles.CylinderHandleCap))
//					{
//						connection.Vehicles[index] = !connection.Vehicles[index];
//						EditorUtility.SetDirty(connection);
//					};
//					index++;
//				}
//			}
		}

		private void OnEnable()
		{
			AssetPreview.SetPreviewTextureCacheSize(IconSize);
		}

		private const int IconSize = 128;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.BeginVertical();

			if (targets.Length > 1)
			{
				if (GUILayout.Button("Merge Connectors"))
				{
					var connectors = targets.OfType<RoadSegment>()
						.SelectMany(segment => segment.Connectors).ToArray();
					foreach (var connector in connectors)
					{
						if (connector.ConnectedTo == null)
						{
							foreach (var other in connectors)
							{
								if (other != connector && other.ConnectedTo == null &&
								    ConfigConstants.Connected(other.transform, connector.transform))
								{
									//TODO provide correct index for edge case usability
									Connect(connector, 0, other, 0);
								}
							}
						}
					}
				}
			}
			else
			{
				RoadSegment segment = (RoadSegment) target;

				if (GUILayout.Button("Populate"))
				{
//					segment.Connectors = segment.gameObject.GetComponentsInChildren<Connector>();
					if (segment.Config == null)
					{
						segment.Config = Resources.FindObjectsOfTypeAll<CityConfig>().First();
					}

					EditorUtility.SetDirty(segment);
				}

				int perRow = Screen.width / IconSize;
				if (_selectedConnector != null)
				{
					var validSegments = segment.Config.Segments.Where(seg =>
						seg.Connectors.Any(con => con.ConnectorType.Compatible(_selectedConnector.ConnectorType)));
					int buttonId = 0;
					foreach (var other in validSegments)
					{
						if (buttonId % perRow == 0) EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button(AssetPreview.GetAssetPreview(other.gameObject), GUILayout.Width(IconSize), GUILayout.Height(IconSize)))
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
								if (_selectedConnector.ConnectorType.Compatible(other.Connectors[index].ConnectorType))
								{
									break;
								}
							}

							PlaceSegment(segment.transform, _selectedConnector, other, index);
						}

						if (buttonId % perRow == perRow - 1) EditorGUILayout.EndHorizontal();
						buttonId++;
					}

					if (buttonId != perRow) EditorGUILayout.EndHorizontal();
					Repaint();
//				int selected = GUILayout.SelectionGrid(selected,
//					validSegments.Select(seg => AssetPreview.GetAssetPreview(seg.gameObject)).ToArray(), 5,
//					GUILayout.Width(IconSize), GUILayout.Height(IconSize));
				}
			}

			EditorGUILayout.EndVertical();
		}

		public void PlaceSegment(Transform curSegment, Connector myConnector, RoadSegment otherSegment,
			int otherConnectorId)
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
			newSegment.transform.position =
				position - newSegment.transform.rotation * otherConnector.transform.localPosition;

			Connect(myConnector, otherConnectorId, otherConnector, _selectedIndex);

			Undo.RecordObject(newSegment.gameObject, "Newly connect Segment");
		}

		private void Connect(Connector conA, int aIndex, Connector conB, int bIndex)
		{
			conA.ConnectedTo = conB;
			conA.ConnectedToIndex = aIndex;
			conB.ConnectedTo = conA;
			conB.ConnectedToIndex = bIndex;
			
			conA.ConnectNodes();
			
			EditorUtility.SetDirty(conA);
			EditorUtility.SetDirty(conB);
			foreach (var node in conA.SharedNodes.Concat(conB.SharedNodes))
			{
				EditorUtility.SetDirty(node);
			}
		}
	}
}
using Config.Proxy;
using Model.Components;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;

namespace Config
{
	public class Connection : BaseGenerator
	{
		public Node StartNode;
		public Node EndNode;
		public int Level = 1;

		public float CurveIn = 1.0f;

		public GameObjectEntity LinkedStartNode;
		public GameObjectEntity LinkedEndNode;

		public Vector3 GetMidPoint()
		{
			if (StartNode == null || EndNode == null)
			{
				return Vector3.zero;
			}
			else
			{
				return (StartNode.transform.position + EndNode.transform.position) * 0.5f;
			}
		}

		private const float SegmentLen = 2f;

		private Spline PreviewBezier()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			Spline ret;
			ret.a = start.position;
			ret.b = (Vector3) ret.a + (start.forward * CurveIn);
			ret.d = end.position;
			ret.c = (Vector3) ret.d - (end.forward * CurveIn);
			return ret;
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = LinkedStartNode.Entity,
				EndNode = LinkedEndNode.Entity,
				Speed = 18.0f / 60f,
				Level = Level,
			};
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (StartNode != null && EndNode != null)
			{
				if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				{
					DrawConnection(false);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			DrawConnection(true);
		}

		private void DrawConnection(bool selected)
		{
			var s = PreviewBezier();
			int length = (int) (s.TotalLength() / SegmentLen);
			for (int i = 0; i <= length - 1; i++)
			{
				var startPoint = (Vector3) s.Point((float) i / length);
				var endPoint = (Vector3) s.Point((float) (i + 1) / length);
				Gizmos.color = selected ? Color.cyan : ((i % 2) == 0 ? Color.green : Color.red);
				Gizmos.DrawLine(startPoint + ConfigConstants.OffsetZ,
					endPoint + ConfigConstants.OffsetZ);
			}

			Gizmos.color = selected ? Color.cyan : Color.white;
			var center = s.Point(0.5f);
			var forward = s.Tangent(0.5f);
			Gizmos.DrawMesh(GetConfig.ConeMesh, center, Quaternion.LookRotation(forward),
				new Vector3(1f, 1f, 2f));
		}

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<GameObjectEntity>();
			LinkedStartNode = StartNode.GenTimePointer.GetComponent<GameObjectEntity>();
			LinkedEndNode = EndNode.GenTimePointer.GetComponent<GameObjectEntity>();
			gameObject.AddComponent<SplineProxy>().Value = ComputeBezierPoints();
			gameObject.AddComponent<ConnectionLengthProxy>().Value = new ConnectionLength
			{
				Length = ComputeLength(),
			};
			var trafficType = GetComponentInParent<RoadSegment>().GetConnectionTrafficType(this);
			gameObject.AddComponent<ConnectionTrafficProxy>().Value = new ConnectionTraffic
			{
				TrafficType = trafficType,
			};
		}

		private Spline ComputeBezierPoints()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			Spline ret;
			ret.a = StartNode.GenTimePointer.transform.position; //use NodePointer here for precise transition
			ret.b = (Vector3) ret.a + (start.forward * CurveIn);
			ret.d = EndNode.GenTimePointer.transform.position;
			ret.c = (Vector3) ret.d - (end.forward * CurveIn);
			return ret;
		}

		private float ComputeLength()
		{
			return ComputeBezierPoints().TotalLength();
		}
#endif
	}
}
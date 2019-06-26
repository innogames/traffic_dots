using Config.Proxy;
using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	public class Connection : BaseGenerator
	{
		public Node StartNode;
		public Node EndNode;
		public int Level = 1;
		
		public float CurveIn = 1.0f;

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
		
		private void OnDrawGizmosSelected()
		{
			DrawSpline();
		}

		private void DrawSpline()
		{
			if (StartNode != null && EndNode != null)
			{
				var offset = Vector3.up * 0.1f; //to avoid z fighting
				var s = PreviewBezier();
				int length = (int) s.TotalLength();
				for (int i = 0; i <= length - 1; i++)
				{
					var startPoint = s.Point((float) i / length);
					var endPoint = s.Point((float) (i + 1) / length);
					Gizmos.color = (i % 2) == 0 ? Color.blue : Color.red;
					Gizmos.DrawLine((Vector3)startPoint + offset, (Vector3)endPoint + offset);
				}
			}
		}

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

#if UNITY_EDITOR

		public GameObjectEntity LinkedStartNode;
		public GameObjectEntity LinkedEndNode;

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<GameObjectEntity>();
			LinkedStartNode = StartNode.GenTimePointer.GetComponent<GameObjectEntity>();
			LinkedEndNode = EndNode.GenTimePointer.GetComponent<GameObjectEntity>();
			gameObject.AddComponent<SplineProxy>().Value = ComputeBezierPoints();
			float conLength = ComputeLength();
			gameObject.AddComponent<ConnectionLengthProxy>().Value = new ConnectionLength
			{
				Length = conLength,
			};
			var trafficType = GetComponentInParent<RoadSegment>().GetConnectionTrafficType(this);
			gameObject.AddComponent<ConnectionTrafficProxy>().Value = new ConnectionTraffic
			{
				TrafficType = trafficType,
			};
			gameObject.AddComponent<ConnectionStateProxy>().Value = new ConnectionState
			{
				EnterLength = conLength,
			};
			gameObject.AddComponent<ConnectionStateAdjustProxy>().Value = new ConnectionStateAdjust
			{
				MoveForward = 0f,
				WillRemoveAgent = false,
			};
			gameObject.AddComponent<AgentQueueBufferProxy>();
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = LinkedStartNode.Entity,
				EndNode = LinkedEndNode.Entity,
				Speed = 12.0f / 60f,
				Level = Level,
			};
		}

		private float ComputeLength()
		{
			return ComputeBezierPoints().TotalLength();
		}
#endif
	}
}
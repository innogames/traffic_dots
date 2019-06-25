using System;
using System.Collections.Generic;
using System.Linq;
using Model.Components;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	public class Connection : BaseGenerator
	{
		public Node StartNode;
		public Node EndNode;
		public bool IsIntersection;
		public int Level = 1;

		private int SlotCount => Vehicles.Count;

		public int LaneCount = 1;
		public float CurveIn = 1.0f;

		public List<bool> Vehicles = new List<bool>();

		private IReadOnlyList<Mesh> _meshes;

		private void GetMeshes()
		{
			if (_meshes == null)
			{
				var config = GetComponentInParent<RoadSegment>().Config;
				if (config == null || config.Vehicles == null) return;
				_meshes = config.Vehicles
					.Select(vehicle => vehicle.GetComponent<MeshFilter>().sharedMesh).ToArray();
			}
		}

		private void OnDrawGizmosSelected()
		{
			DrawVehicle(false, Color.gray);
		}

//		private void OnDrawGizmos()
//		{
//			DrawVehicle(true, Color.green);
//		}

		private void DrawVehicle(bool vehicleEnable, Color color)
		{
			if (StartNode != null && EndNode != null)
			{
				var s = PreviewBezier();
				var length = (int) s.TotalLength();
				for (int i = 0; i <= length - 1; i++)
				{
					var startPoint = s.Point((float) i / length);
					var endPoint = s.Point((float) (i + 1) / length);
					Gizmos.color = (i % 2) == 0 ? Color.blue : Color.red;
					Gizmos.DrawLine(startPoint, endPoint);
				}

//				Gizmos.color = color;
//				GetMeshes();
//
//				using (var tangents = SlotSteps(BezierFunc(true)).GetEnumerator())
//				{
//					int index = 0;
//					foreach (var pos in SlotSteps(BezierFunc()))
//					{
//						tangents.MoveNext();
//						if (Vehicles[index] == vehicleEnable)
//						{
//							var forward = tangents.Current;
//							var mesh = _meshes[Math.Abs(pos.GetHashCode()) % _meshes.Count];
//							Gizmos.DrawMesh(mesh, pos, Quaternion.LookRotation(forward, Vector3.up));
//						}
//
//						index++;
//					}
//				}
			}
		}

		public Func<float, Vector3> BezierFunc(bool isTangent = false)
		{
			var s = ComputeBezierPoints();
			if (isTangent)
			{
				return t => (Vector3) s.Tangent(t);
			}
			else
			{
				return t => (Vector3) s.Point(t);
			}
		}

		private Spline PreviewBezier()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			Spline ret;
			ret.a = start.position; //use NodePointer here for precise transition
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
			ret.a = StartNode.NodePointer.transform.position; //use NodePointer here for precise transition
			ret.b = (Vector3) ret.a + (start.forward * CurveIn);
			ret.d = EndNode.NodePointer.transform.position;
			ret.c = (Vector3) ret.d - (end.forward * CurveIn);
			return ret;
		}

		public IEnumerable<Vector3> SlotSteps(Func<float, Vector3> func)
		{
			for (int i = 0; i < SlotCount; i++)
			{
				float t = (i + 0.5f) / SlotCount;
				yield return func(t);
			}
		}

#if UNITY_EDITOR

		public GameObjectEntity LinkedStartNode;
		public GameObjectEntity LinkedEndNode;

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<GameObjectEntity>();
			LinkedStartNode = StartNode.NodePointer.GetComponent<GameObjectEntity>();
			LinkedEndNode = EndNode.NodePointer.GetComponent<GameObjectEntity>();
			gameObject.AddComponent<SplineProxy>().Value = ComputeBezierPoints();
//			gameObject.AddComponent<EntitySlotProxy>().Value = new EntitySlot
//			{
//				SlotCount = SlotCount,
//			};
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = LinkedStartNode.Entity,
				EndNode = LinkedEndNode.Entity,
				Speed = 6.0f / 60f,
				Level = Level,
				Length = ComputeLength(),
			};
		}

		private float ComputeLength()
		{
			return ComputeBezierPoints().TotalLength();
		}
#endif
	}
}
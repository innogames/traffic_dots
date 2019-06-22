using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Config
{
	public class Connection : MonoBehaviour
	{
		public Node StartNode;
		public Node EndNode;
		public int Level = 1;
		public int Slots = 1;
		public int LaneCount = 1;

		private IEnumerable<Mesh> _meshes;

		private void GetMeshes()
		{
			if (_meshes == null)
			{
				var config = GetComponentInParent<RoadSegment>().Config;
				if (config == null || config.Vehicles == null) return;
				_meshes = config.Vehicles
					.Select(vehicle => vehicle.GetComponent<MeshFilter>().sharedMesh);
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (StartNode != null && EndNode != null)
			{
				Gizmos.color = Color.red;
				GetMeshes();

				using (var tangents = SlotSteps(TangentFunc()).GetEnumerator())
				{
					foreach (var pos in SlotSteps(BezierFunc()))
					{
						tangents.MoveNext();
						var forward = tangents.Current;
						Gizmos.DrawWireMesh(_meshes.First(), pos, Quaternion.LookRotation(forward, Vector3.up));
					}
				}
			}
		}

		public Func<float, Vector3> BezierFunc()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			var p0 = start.position;
			var p1 = p0 + Vector3.Scale(start.forward, start.localScale);
			var p3 = end.position;
			var p2 = p3 - Vector3.Scale(end.forward, end.localScale);
			return t =>
			{
				float t1 = 1 - t;
				float t12 = t1 * t1;
				float t13 = t1 * t1 * t1;
				return t13 * p0
				       + 3 * t12 * t * p1
				       + 3 * t1 * t * t * p2
				       + t * t * t * p3;
			};
		}

		public Func<float, Vector3> TangentFunc()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			var p0 = start.position;
			var p1 = p0 + Vector3.Scale(start.forward, start.localScale);
			var p3 = end.position;
			var p2 = p3 - Vector3.Scale(end.forward, end.localScale);
			return t =>
			{
				float t1 = 1 - t;
				float t12 = t1 * t1;
				return -3 * t12 * p0
				       + (3 * t12 - 6 * t1 * t) * p1
				       + (-3 * t * t + 6 * t * t1) * p2
				       + 3 * t * t * p3;
			};
		}

		public IEnumerable<Vector3> SlotSteps(Func<float, Vector3> func)
		{
			for (int i = 0; i < Slots; i++)
			{
				float t = (i + 0.5f) / Slots;
				yield return func(t);
			}
		}

		private void Awake()
		{
			gameObject.AddComponent<GameObjectEntity>();
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = StartNode.NodePointer.GetComponent<GameObjectEntity>().Entity,
				EndNode = EndNode.NodePointer.GetComponent<GameObjectEntity>().Entity,
				Cost = 1.0f,
				Level = Level,
			};
		}
	}
}
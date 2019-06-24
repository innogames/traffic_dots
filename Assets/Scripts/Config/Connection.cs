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

		private void OnDrawGizmos()
		{
			DrawVehicle(true, Color.green);
		}

		private void DrawVehicle(bool vehicleEnable, Color color)
		{
			if (StartNode != null && EndNode != null)
			{
				Gizmos.color = color;
				GetMeshes();

				using (var tangents = SlotSteps(BezierFunc(true)).GetEnumerator())
				{
					int index = 0;
					foreach (var pos in SlotSteps(BezierFunc()))
					{
						tangents.MoveNext();
						if (Vehicles[index] == vehicleEnable)
						{
							var forward = tangents.Current;						
							var mesh = _meshes[Math.Abs(pos.GetHashCode()) % _meshes.Count];
							Gizmos.DrawMesh(mesh, pos, Quaternion.LookRotation(forward, Vector3.up));							
						}
						index++;
					}
				}
			}
		}

		public Func<float, Vector3> BezierFunc(bool isTangent = false)
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			var p0 = start.position;
			var p1 = p0 + start.forward * CurveIn;
			var p3 = end.position;
			var p2 = p3 - end.forward * CurveIn;
			if (isTangent)
			{
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
			else
			{
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
		
		public virtual void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<GameObjectEntity>();
			LinkedStartNode = StartNode.NodePointer.GetComponent<GameObjectEntity>();
			LinkedEndNode = EndNode.NodePointer.GetComponent<GameObjectEntity>();
			gameObject.AddComponent<SplineProxy>().Value = new Spline
			{
				a = StartNode.NodePointer.transform.position,
				b = Vector3.zero,
				c = Vector3.zero,
				d = EndNode.NodePointer.transform.position,
			};
			gameObject.AddComponent<EntitySlotProxy>().Value = new EntitySlot
			{
				SlotCount = SlotCount,
			};
		}

		public override void PlayModeGenerate(CityConfig config)
		{
			base.PlayModeGenerate(config);
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = LinkedStartNode.Entity,
				EndNode = LinkedEndNode.Entity,
				Speed = 1.0f,
				Level = Level,
			};
		}
		#endif
	}
}
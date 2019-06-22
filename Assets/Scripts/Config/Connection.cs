using System;
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

		private void OnDrawGizmosSelected()
		{
			if (StartNode != null && EndNode != null)
			{
				Gizmos.color = Color.red;
				var bezierFunc = BezierFunc();
				var lastPos = StartNode.transform.position;
				float lastT = 0f;
				for (int i = 1; i <= Slots; i++)
				{
					float t = (float)i / Slots;
					var pos = bezierFunc(t);
					Gizmos.DrawLine(lastPos, pos);
					Gizmos.DrawCube(bezierFunc((t+lastT) * 0.5f), Vector3.one);						
					lastPos = pos;
					lastT = t;
				}
			}
		}

		private Func<float, Vector3> BezierFunc()
		{
			var start = StartNode.transform;
			var end = EndNode.transform;
			var p0 = start.position;
			var p1 = p0 + Vector3.Scale(start.forward, start.localScale);
			var p3 = end.position;
			var p2 = p3 - Vector3.Scale(end.forward, end.localScale);
			return t =>
			{
				var t1 = 1 - t;
				var t12 = t1 * t1;
				var t13 = t1 * t1 * t1;
				return t13 * p0
				       + 3 * t12 * t * p1
				       + 3 * t1 * t * t * p2
				       + t * t * t * p3;
			};
		}

		private void Awake()
		{
			gameObject.AddComponent<GameObjectEntity>();
			var connection = gameObject.AddComponent<ConnectionProxy>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = StartNode.GetComponent<GameObjectEntity>().Entity,
				EndNode = EndNode.GetComponent<GameObjectEntity>().Entity,
				Cost = 1.0f,
				Level = Level,
			};
		}
	}
}
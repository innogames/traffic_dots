using Config.Proxy;
using Model.Components;
using Model.Components.Buffer;
using Unity.Entities;
#if UNITY_EDITOR
using Unity.Mathematics;
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

		public float CurveIn = 1.0f;

		public GameObjectEntity LinkedStartNode;
		public GameObjectEntity LinkedEndNode;
		public float CachedSpeed = 1f;

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
			var roadSegment = GetComponentInParent<RoadSegment>();
			connection.Value = new Model.Components.Connection
			{
				StartNode = LinkedStartNode.Entity,
				EndNode = LinkedEndNode.Entity,
				Speed = CachedSpeed.ToCityInt(), //TODO merge this with ConnectionSpeedInt
				Level = roadSegment == null ? 1 : roadSegment.Level,
			};
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (StartNode != null && EndNode != null)
			{
				if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				{
					DrawConnection(DrawMode.Line | DrawMode.Arrow);
				}
				else if (Application.isPlaying)
				{
					DrawConnection(DrawMode.Arrow | DrawMode.NetColor);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			var mode = DrawMode.Selected | DrawMode.Line | DrawMode.Arrow;
			if (Application.isPlaying) mode |= DrawMode.NetColor;
			DrawConnection(mode);
			if (Selection.objects.Length == 1)
			{
				DrawPlayingGizmo();
			}
		}

		[System.Flags]
		private enum DrawMode
		{
			None = 0,
			Selected = 1 << 0,
			Line = 1 << 1,
			Arrow = 1 << 2,
			NetColor = 1 << 3,
			Darker = 1 << 4,
		}

		private int GetNetworkId()
		{
			if (!Application.isPlaying) return 0;
			if (World.Active == null) return 0;
			var entityManager = World.Active.EntityManager;
			var entity = GetComponent<GameObjectEntity>().Entity;
			if (!entityManager.HasComponent<NetworkGroupState>(entity)) return 0;
			return entityManager.GetComponentData<NetworkGroupState>(entity).NetworkId;
		}

		private void DrawConnection(DrawMode mode)
		{
			var s = PreviewBezier();
			DrawSpline(mode, s, GetNetworkId());
		}

		private void DrawSpline(DrawMode mode, Spline s, int netId)
		{
			int length = (int) (s.TotalLength() / SegmentLen);
			bool selected = (mode & DrawMode.Selected) != 0;
			if ((mode & DrawMode.Line) != 0)
			{
				bool useNetColor = (mode & DrawMode.NetColor) != 0;
				var netColor = Color.blue;
				if (useNetColor)
				{
					var colors = GetConfig.NetworkColors;
					netColor = colors[netId % colors.Length];
				}

				for (int i = 0; i <= length - 1; i++)
				{
					bool isEven = i % 2 == 0;
					var startPoint = (Vector3) s.Point((float) i / length);
					var endPoint = (Vector3) s.Point((float) (i + 1) / length);
					var color = useNetColor
						? netColor
						: (selected
							? (isEven ? Color.red : Color.green)
							: Color.white);
					if ((mode & DrawMode.Darker) != 0) color *= 0.75f;
					Gizmos.color = color;
					Gizmos.DrawLine(startPoint + ConfigConstants.OffsetZ,
						endPoint + ConfigConstants.OffsetZ);
				}
			}

			if ((mode & DrawMode.Arrow) != 0)
			{
				Gizmos.color = selected ? Color.cyan : Color.white;
				var center = s.Point(0.5f);
				var forward = s.Tangent(0.5f);
				Gizmos.DrawMesh(GetConfig.ConeMesh, center, Quaternion.LookRotation(forward),
					new Vector3(1f, 1f, 2f));
			}
		}

		private void DrawPlayingGizmo()
		{
			if (!Application.isPlaying) return;
			if (World.Active == null) return;
			var entityManager = World.Active.EntityManager;
			var entity = GetComponent<GameObjectEntity>().Entity;
			DrawEntranceExit(entityManager, entity);
			DrawOnlyNext(entityManager, entity);
			DrawNextPos(entityManager, entity);
		}

		private void DrawEntranceExit(EntityManager entityManager, Entity entity)
		{
			if (!entityManager.HasComponent<NetPathInfo>(entity)) return;
			var netPathInfo = entityManager.GetComponentData<NetPathInfo>(entity);

			if (netPathInfo.NearestEntrance != Entity.Null)
			{
				Gizmos.color = Color.magenta;
				var entrancePos = entityManager.GetComponentData<Model.Components.Node>(netPathInfo.NearestEntrance)
					.Position;
				Gizmos.DrawCube(entrancePos, Vector3.one);
				Gizmos.DrawLine(entrancePos, StartNode.transform.position);
			}

			if (netPathInfo.NearestExit != Entity.Null)
			{
				Gizmos.color = Color.magenta;
				var exitPos = entityManager.GetComponentData<Model.Components.Node>(netPathInfo.NearestExit).Position;
				Gizmos.DrawCube(exitPos, Vector3.one);
				Gizmos.DrawLine(EndNode.transform.position, exitPos);
			}
		}

		private void DrawNextPos(EntityManager entityManager, Entity entity)
		{
			if (!entityManager.HasComponent<NextBuffer>(entity)) return;
			if (!entityManager.HasComponent<Model.Components.Connection>(entity)) return;
//			if (entityManager.GetComponentData<Model.Components.Connection>(entity).OnlyNext != Entity.Null) return;
			var netGroup = entityManager.GetComponentData<Model.Components.NetworkGroupState>(entity);
			var indexToTarget = entityManager.GetBuffer<IndexToTargetBuffer>(netGroup.Network);
			var buffer = entityManager.GetBuffer<NextBuffer>(entity);
			for (int i = 0; i < buffer.Length; i++)
			{
				var nextCon = buffer[i].Connection;
				if (nextCon == Entity.Null) continue;
				var target = indexToTarget[i].Target;
				var pos = new float3(0,0,0);
				if (entityManager.HasComponent<Connection>(target))
				{
					var node = entityManager.GetComponentData<Model.Components.Connection>(target).EndNode;
					pos = entityManager.GetComponentData<Model.Components.Node>(node).Position;
				}

				if (entityManager.HasComponent<Model.Components.Node>(target))
				{
					pos = entityManager.GetComponentData<Model.Components.Node>(target).Position;
				}
				Gizmos.color = Color.red;
				Gizmos.DrawLine(EndNode.transform.position, pos);
				Gizmos.DrawSphere(pos, 0.25f);
			}
		}

		private void DrawOnlyNext(EntityManager entityManager, Entity entity)
		{
			if (!entityManager.HasComponent<Model.Components.Connection>(entity)) return;
			var connection = entityManager.GetComponentData<Model.Components.Connection>(entity);
			if (connection.OnlyNext != Entity.Null)
			{
				var next = connection.OnlyNext;
				int netId = entityManager.GetComponentData<NetworkGroupState>(next).NetworkId;
				var spline = entityManager.GetComponentData<Spline>(next);
				DrawSpline(DrawMode.Line | DrawMode.NetColor | DrawMode.Darker, spline, netId);
			}
		}

		public override void Generate(CityConfig config)
		{
			base.Generate(config);
			gameObject.AddComponent<GameObjectEntity>();
			LinkedStartNode = StartNode.GenTimePointer.GetComponent<GameObjectEntity>();
			LinkedEndNode = EndNode.GenTimePointer.GetComponent<GameObjectEntity>();
			gameObject.AddComponent<SplineProxy>().Value = ComputeBezierPoints();
			var trafficType = GetComponentInParent<RoadSegment>().GetConnectionTrafficType(this);
			gameObject.AddComponent<ConnectionTrafficProxy>().Value = new ConnectionTraffic
			{
				TrafficType = trafficType,
			};
			int conLen = ComputeLength().ToCityInt();
			gameObject.AddComponent<ConnectionLengthIntProxy>().Value = new ConnectionLengthInt
			{
				Length = conLen,
			};
			gameObject.AddComponent<ConnectionStateIntProxy>().Value = new ConnectionStateInt
			{
				EnterLen = conLen,
			};
			gameObject.AddComponent<ConnectionPullIntProxy>().Value = new ConnectionPullInt
			{
				Pull = 0,
			};
			gameObject.AddComponent<ConnectionPullQIntProxy>().Value = new ConnectionPullQInt
			{
				PullQ = 0,
			};
			gameObject.AddComponent<NetworkGroupStateProxy>().Value = new NetworkGroupState
			{
				NetworkId = -1,
			};
			CachedSpeed = config.ConnectionBaseSpeed / config.TargetFramerate *
			              GetComponentInParent<RoadSegment>().SpeedMultiplier;
			gameObject.AddComponent<ConnectionSpeedIntProxy>().Value = new ConnectionSpeedInt
			{
				Speed = CachedSpeed.ToCityInt(),
			};

			var marker = gameObject.GetComponent<TargetMarker>();
			if (marker != null)
			{
				gameObject.AddComponent<TargetProxy>().Value = new Target
				{
					TargetMask = (int) marker.TargetMask,
				};
			}
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
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Intersection : IComponentData
	{
		public int X;
		public int Y;
	}

	public struct Occupant : IComponentData
	{
	}

	public struct Mover : IComponentData
	{
		public Entity Destination; //Node
	}

	public struct HexaNode : IComponentData
	{
	}

	[Serializable]
	public struct RoadSegment : IComponentData
	{
		public int LaneCount;
		public bool IsVertical;
		public int X;
		public int Y;
	}

	public struct RoadSegmentState : ISystemStateComponentData
	{
		public Entity IntersectionStart;
		public Entity IntersectionEnd;
		public int SegmentIndex;
	}

	public struct RoadIntersection : IComponentData
	{
	}

	public struct RoadVehicle : IComponentData
	{
		public float Velocity;
		public Entity Road;
	}
}
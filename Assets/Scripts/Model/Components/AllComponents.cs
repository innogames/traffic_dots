using System;
using Unity.Entities;

namespace Model.Components
{
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

	public struct RoadSegmentState : ISystemStateComponentData
	{
		public Entity IntersectionStart;
		public Entity IntersectionEnd;
		public int SegmentIndex;
	}

	public struct RoadVehicle : IComponentData
	{
		public float Velocity;
		public Entity Road;
	}
}
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	[Serializable]
	public struct Node: IComponentData
	{
		public float3 position;
	}

	[Serializable]
	public struct Connection : IComponentData
	{
		public float weight;
		public Entity startNode;
		public Entity endNode;
	}
	
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

	public struct RoadVehicle : IComponentData
	{
		public float Velocity;
		public Entity Road;
	}
}
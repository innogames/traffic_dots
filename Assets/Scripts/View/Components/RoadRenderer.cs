using System;
using Unity.Entities;
using Unity.Rendering;

namespace View.Components
{
	[Serializable]
	public struct RoadRenderer : ISharedComponentData, IEquatable<RoadRenderer>
	{
		public RenderMesh RoadSegment;
		public RenderMesh Intersection;

		public bool Equals(RoadRenderer other)
		{
			return RoadSegment.Equals(other.RoadSegment) && Intersection.Equals(other.Intersection);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is RoadRenderer other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (RoadSegment.GetHashCode() * 397) ^ Intersection.GetHashCode();
			}
		}
	}
}
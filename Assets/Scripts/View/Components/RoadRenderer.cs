using System;
using Unity.Entities;
using Unity.Rendering;

namespace View.Components
{
	[Serializable]
	public struct RoadRenderer : ISharedComponentData
	{
		public RenderMesh RoadSegment;
		public RenderMesh Intersection;
	}
}
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct TailCoord : IComponentData
	{
		public Entity Connection;
		public float Coord;
	}
}
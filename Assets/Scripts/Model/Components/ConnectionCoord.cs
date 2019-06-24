using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionCoord : IComponentData
	{
		public Entity Connection;
		public float Coord;
	}
}
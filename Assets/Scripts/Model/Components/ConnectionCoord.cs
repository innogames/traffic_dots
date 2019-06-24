using Unity.Entities;

namespace Model.Components
{
	public struct ConnectionCoord : IComponentData
	{
		public Entity Connection;
		public float Coord;
	}
}
using Unity.Entities;

namespace Model.Components
{
	public struct ConnectionDestination : IComponentData
	{
		public Entity Connection;
		public int Slot;
	}
}
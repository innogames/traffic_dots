using Unity.Entities;

namespace Model.Components
{
	public struct ConnectionLocation : IComponentData
	{
		public Entity Connection;
		public int Slot;
	}
}
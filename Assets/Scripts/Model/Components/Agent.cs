using Unity.Entities;

namespace Model.Components
{
	public struct Agent : IComponentData
	{
		public float Speed;
		public Entity Connection;
	}
}
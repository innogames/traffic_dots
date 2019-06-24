using Unity.Entities;

namespace Model.Components
{
	public struct ConnectionTarget : IComponentData
	{
		public Entity Connection;
	}
}
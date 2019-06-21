using Unity.Entities;

namespace Model.Components
{
	public struct PathIntent : IComponentData
	{
		public Entity EndNode;
	}
}
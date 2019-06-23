using Unity.Entities;

namespace Model.Components
{
	public struct AgentSpawner : ISharedComponentData
	{
		public Entity Agent;
	}
}
using Unity.Entities;

namespace Model.Systems.City
{
	public struct Agent : IComponentData
	{
		public float Speed;
		public Entity Connection;
	}

	public struct PathIntent : IComponentData
	{
		public Entity EndNode;
	}

	public struct AgentData : ISystemStateComponentData
	{
		public float Lerp;
		public int Slot;
	}
}
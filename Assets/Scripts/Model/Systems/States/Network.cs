using Unity.Entities;

namespace Model.Systems.States
{
	public struct Network : ISystemStateComponentData
	{
		public int Index;
	}

	public struct NetworkDone : ISystemStateComponentData
	{
		public int Something;
	}
}
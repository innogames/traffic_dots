using Unity.Entities;

namespace Model.Systems
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
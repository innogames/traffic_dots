using Unity.Entities;

namespace Model.Systems
{
	public struct IndexInNetwork : ISystemStateComponentData
	{
		public int Index;
	}
}
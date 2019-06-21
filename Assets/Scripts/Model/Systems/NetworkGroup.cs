using Unity.Entities;

namespace Model.Systems
{
	public struct NetworkGroup : ISharedComponentData
	{
		public int NetworkId;		
	}
}
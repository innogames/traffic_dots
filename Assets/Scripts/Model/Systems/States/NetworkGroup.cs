using Unity.Entities;

namespace Model.Systems.States
{
	public struct NetworkGroup : ISharedComponentData
	{
		public int NetworkId;		
	}
}
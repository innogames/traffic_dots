using Unity.Entities;

namespace Model.Systems
{
	public struct NextBuffer : IBufferElementData
	{
		public Entity Connection;
	}
}
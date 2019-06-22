using Unity.Entities;

namespace Model.Systems
{
	[InternalBufferCapacity(SystemConstants.NetworkNodeSize)]
	public struct NextBuffer : IBufferElementData
	{
		public Entity Connection;
	}
}
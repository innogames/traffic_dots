using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(SystemConstants.NetworkNodeSize)]
	public struct NextBuffer : IBufferElementData
	{
		public Entity Connection;
	}
}
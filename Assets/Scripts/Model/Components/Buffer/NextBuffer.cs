using Unity.Entities;

namespace Model.Components.Buffer
{
	[InternalBufferCapacity(SystemConstants.NetworkNodeSize)]
	public struct NextBuffer : IBufferElementData
	{
		public Entity Connection;
	}
}
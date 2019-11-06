using Unity.Entities;

namespace Model.Components.Buffer
{
	[InternalBufferCapacity(SystemConstants.NetworkNodeSize)]
	public struct IndexToTargetBuffer : IBufferElementData
	{
		public Entity Target;
	}
}
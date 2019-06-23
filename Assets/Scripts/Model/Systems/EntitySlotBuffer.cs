using Unity.Entities;

namespace Model.Systems
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct EntitySlotBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
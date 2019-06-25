using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct EntitySlotBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
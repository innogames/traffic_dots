using Unity.Entities;

namespace Model.Systems
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct AgentQueueBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
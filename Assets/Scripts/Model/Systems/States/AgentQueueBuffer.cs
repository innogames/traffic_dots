using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct AgentQueueBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
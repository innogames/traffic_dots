using System;
using Unity.Entities;

namespace Model.Components.Buffer
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.ConnectionSlotAverage)]
	public struct AgentQueueBuffer : IBufferElementData
	{
		public Entity Agent;

		public static implicit operator AgentQueueBuffer(Entity agent)
		{
			return new AgentQueueBuffer {Agent = agent};
		}
	}
}
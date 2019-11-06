using Model.Components;
using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(ComponentConstants.ConnectionSlotAverage)]
	public struct EntitySlotBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
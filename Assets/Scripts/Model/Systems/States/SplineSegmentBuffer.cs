using Model.Components;
using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(ComponentConstants.ConnectionSlotAverage)]
	public struct SplineSegmentBuffer : IBufferElementData
	{
		public int Length;
	}
}
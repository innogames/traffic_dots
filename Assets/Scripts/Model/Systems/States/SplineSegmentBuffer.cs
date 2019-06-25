using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct SplineSegmentBuffer : IBufferElementData
	{
		public int Length;
	}
}
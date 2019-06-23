using Unity.Entities;

namespace Model.Systems
{
	[InternalBufferCapacity(SystemConstants.ConnectionSlotAverage)]
	public struct SplineSegmentBuffer : IBufferElementData
	{
		public int Length;
	}
}
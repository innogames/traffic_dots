using Unity.Entities;

namespace Model.Components.Buffer
{
	[InternalBufferCapacity(ComponentConstants.SpanwerBufferAverage)]
	public struct SpawnerBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
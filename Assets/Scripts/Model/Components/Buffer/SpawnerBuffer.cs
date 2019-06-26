using System;
using Unity.Entities;

namespace Model.Components.Buffer
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.SpanwerBufferAverage)]
	public struct SpawnerBuffer : IBufferElementData
	{
		public Entity Agent;
	}
}
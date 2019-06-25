using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.IntersectionPhase)]
	public struct IntersectionPhaseBuffer : IBufferElementData
	{
		public Entity Connection;
		public int Frames;
	}
}
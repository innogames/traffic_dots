using System;
using Unity.Entities;

namespace Model.Components.Buffer
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.IntersectionPhase)]
	public struct IntersectionPhaseBuffer : IBufferElementData
	{
		public int StartIndex;
		public int EndIndex;
		public int Frames;
	}
}
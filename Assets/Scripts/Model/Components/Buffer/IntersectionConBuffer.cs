using System;
using Unity.Entities;

namespace Model.Components.Buffer
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.IntersectionConnection)]
	public struct IntersectionConBuffer : IBufferElementData
	{
		public Entity Connection;
	}
}
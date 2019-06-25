using System;
using Unity.Entities;

namespace Model.Components.Buffer
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.TargetNumber)]
	public struct TargetBuffer : IBufferElementData
	{
		public Entity Target;
	}
}
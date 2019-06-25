using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	[InternalBufferCapacity(ComponentConstants.TargetNumber)]
	public struct TargetBuffer : IBufferElementData
	{
		public Entity Target;
	}
}
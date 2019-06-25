using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct TargetSeeker : IComponentData
	{
		public int TargetMask;
		public int LastTargetIndex;
	}
}
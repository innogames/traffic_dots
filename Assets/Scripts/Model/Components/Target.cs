using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Target : IComponentData
	{
		public int TargetMask;
	}
}
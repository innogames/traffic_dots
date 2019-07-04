using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionTargetState : IComponentData
	{
		public Entity NextTarget;
		public int TargetIndex;
	}
}
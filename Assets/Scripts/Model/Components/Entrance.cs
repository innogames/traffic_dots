using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Entrance : IComponentData
	{
		public int NetIdx;
		public Entity Network;
		public int Level;
	}
}
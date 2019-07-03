using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Entrance : IComponentData
	{
		public int NetIdx;
		public int Level;
	}
}
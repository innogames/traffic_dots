using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct EntitySlot : IComponentData
	{
		public int SlotCount;
	}
}
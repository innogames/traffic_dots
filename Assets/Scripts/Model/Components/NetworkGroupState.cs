using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct NetworkGroupState : IComponentData
	{
		public int NetworkId;
	}
}
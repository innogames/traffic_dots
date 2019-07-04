using System;
using Unity.Entities;

namespace Model.Systems.States
{
	[Serializable]
	public struct NetworkGroupState : IComponentData
	{
		public int NetworkId;
	}
}
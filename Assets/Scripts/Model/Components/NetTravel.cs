using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct NetTravel : IComponentData
	{
		public Entity ExitNode;
		public Entity EntranceNode;
	}
}
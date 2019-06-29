using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentInt : IComponentData
	{
		public int Length;
	}
}
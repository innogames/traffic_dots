using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentStateInt : IComponentData
	{
		public AgentState State;
		public int MoveDist;
	}
}
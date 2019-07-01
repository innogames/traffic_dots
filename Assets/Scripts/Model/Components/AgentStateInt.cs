using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentStateInt : IComponentData
	{
		public Entity TailCon;
		public int TailCord;
		public int MoveDist;
	}
}
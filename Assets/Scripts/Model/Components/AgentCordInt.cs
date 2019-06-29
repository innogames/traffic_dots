using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentCordInt : IComponentData
	{
		public Entity HeadCon;
		public int HeadCord;
	}
}
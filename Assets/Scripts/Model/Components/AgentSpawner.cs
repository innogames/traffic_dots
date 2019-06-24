using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentSpawner : ISharedComponentData
	{
		public Entity Agent;
	}
}
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentSpawner : IComponentData
	{
		public int CurrentIndex;
	}
}
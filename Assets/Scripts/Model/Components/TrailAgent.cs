using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct TrailAgent : IComponentData
	{
		public Entity Agent;
	}
}
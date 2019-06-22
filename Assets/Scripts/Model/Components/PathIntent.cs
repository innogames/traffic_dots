using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct PathIntent : IComponentData
	{
		public Entity EndNode;
	}
}
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionTarget : IComponentData
	{
		public Entity Connection;
	}
}
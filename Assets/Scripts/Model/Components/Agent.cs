using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Agent : IComponentData
	{
		public float Speed;
		public Entity Connection;
	}
}
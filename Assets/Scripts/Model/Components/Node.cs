using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	[Serializable]
	public struct Node : IComponentData
	{
		public float3 Position;
		public int Level;
	}
}
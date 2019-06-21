using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	public struct Node : IComponentData
	{
		public float3 Position;
		public int Level;
	}
}
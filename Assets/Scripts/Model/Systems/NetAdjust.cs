using Unity.Entities;

namespace Model.Systems
{
	public struct NetAdjust : IBufferElementData
	{
		public Entity Connection;
		public Entity StartNode;
		public Entity EndNode;
		public float Cost;
	}
}
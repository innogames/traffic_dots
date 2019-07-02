using Unity.Entities;

namespace Model.Systems.States
{
	[InternalBufferCapacity(SystemConstants.NetworkConnectionSize)]
	public struct NetAdjust : IBufferElementData
	{
		public Entity Connection;
		public Entity StartNode;
		public Entity EndNode;
		public float Cost;
		public Entity OnlyNext;
	}
}
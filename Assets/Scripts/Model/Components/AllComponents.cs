using Unity.Entities;

namespace Model.Components
{
	public struct Node : IComponentData
	{
	}

	public struct Occupant : IComponentData
	{
	}

	public struct Mover : IComponentData
	{
		public Entity Destination; //Node
	}

	public struct HexaNode : IComponentData
	{
	}
}
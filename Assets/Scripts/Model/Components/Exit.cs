using System;
using Unity.Entities;

namespace Model.Components
{
	/// <summary>
	/// attached to Node, to indicate that it is an exit
	/// a node can only be an exit for one network (auto true for one-way lane road network)
	/// </summary>
	[Serializable]
	public struct Exit : IComponentData
	{
		public int NetIdx;
		public int Level;
		public int IndexInNetwork;
	}
}
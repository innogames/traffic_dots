using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionPullInt : IComponentData
	{
		public int PullLife;
		public int PullForce;
	}
}
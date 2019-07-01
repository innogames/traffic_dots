using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionPullQInt : IComponentData
	{
		public int PullQ;
	}
}
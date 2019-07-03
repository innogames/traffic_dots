using System;
using Unity.Entities;

namespace Model.Components
{
	/// <summary>
	/// to be attached to connection that is part of a network
	/// </summary>
	[Serializable]
	public struct NetPathInfo : IComponentData
	{
		public Entity NearestExit;
		public Entity NearestEntrance;
	}
}
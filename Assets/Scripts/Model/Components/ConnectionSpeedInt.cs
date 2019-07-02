using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionSpeedInt : IComponentData
	{
		public int Speed;		
	}
}
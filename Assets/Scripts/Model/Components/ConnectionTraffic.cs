using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionTraffic : IComponentData
	{
		public ConnectionTrafficType TrafficType;
	}
}
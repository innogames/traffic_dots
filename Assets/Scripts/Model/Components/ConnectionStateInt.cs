using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionStateInt : IComponentData
	{
		public int EnterLen;
	}
}
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionLengthInt : IComponentData
	{
		public int Length;		
	}
}
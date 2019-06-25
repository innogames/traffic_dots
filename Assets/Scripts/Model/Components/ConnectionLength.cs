using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionLength : IComponentData
	{
		public float Length;		
	}
}
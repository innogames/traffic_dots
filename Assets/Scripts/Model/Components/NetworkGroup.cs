using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct NetworkGroup : ISharedComponentData
	{
		public int NetworkId;		
	}
}
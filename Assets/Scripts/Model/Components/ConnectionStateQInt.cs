using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionStateQInt : IComponentData
	{
		public int EnterLenQ;
	}
}
using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct AgentCoordInt : IComponentData
	{
		public Entity Connection;
		public int CurCoord;

		public Entity TailCon;
		public int CurTail;
	}
}
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionStateAdjust : ISystemStateComponentData
	{
		public float MoveForward;
		public bool WillRemoveAgent;

		public void AgentLeaveThePack(ref Agent agent, ref ConnectionLength conLength)
		{
			MoveForward = math.min(agent.Length, conLength.Length);
		}
	}
}
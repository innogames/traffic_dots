using Model.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems.States
{
	public struct ConnectionState : ISystemStateComponentData
	{
		public float EnterLength;
		public float ExitLength;

		public bool CouldAgentEnter(ref Agent agent, ref ConnectionLength conLength)
		{
			return (EnterLength >= agent.Length //car fit 
			        || EnterLength >= conLength.Length); //car is larger than the road & the road is empty
		}

		public float NewAgentCoord(ref ConnectionLength conLength)
		{
			return conLength.Length - EnterLength;
		}

		public void AcceptAgent(ref Agent agent)
		{
			EnterLength = math.max(0f, EnterLength - agent.Length);
		}

		public int FramesToEnter(ref Connection connection)
		{
			return (int) math.ceil(EnterLength / connection.Speed);
		}

		public void AgentLeaveThePack(ref Agent agent, ref ConnectionLength conLength)
		{
			ExitLength = math.min(ExitLength + agent.Length, conLength.Length);
		}

		public void ClearConnection(ref ConnectionLength conLength)
		{
			EnterLength = conLength.Length;
		}

		public bool IsEmpty(ref ConnectionLength conLength)
		{
			return EnterLength >= conLength.Length;
		}
	}
}
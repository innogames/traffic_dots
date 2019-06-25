using Model.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Systems
{
	public struct ConnectionState : ISystemStateComponentData
	{
		public float EnterLength;
		public float ExitLength;

		public bool CouldAgentEnter(ref Agent agent, ref Connection connection)
		{
			return (EnterLength >= agent.Length //car fit 
			        || EnterLength >= connection.Length); //car is larger than the road & the road is empty
		}

		public float NewAgentCoord(ref Connection connection)
		{
			return connection.Length - EnterLength;
		}

		public void AcceptAgent(ref Agent agent)
		{
			EnterLength = math.max(0f, EnterLength - agent.Length);
		}

		public int FramesToEnter(ref Connection connection)
		{
			return (int) math.ceil(EnterLength / connection.Speed);
		}

		public void AgentLeaveThePack(ref Agent agent, ref Connection connection)
		{
			ExitLength = math.min(ExitLength + agent.Length, connection.Length);
		}

		public void ClearConnection(ref Connection connection)
		{
			EnterLength = connection.Length;
		}
	}
}
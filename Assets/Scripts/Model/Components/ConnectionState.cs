using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	[Serializable]
	public struct ConnectionState : ISystemStateComponentData
	{
		public float EnterLength;

		public bool CouldAgentFullyEnter(ref Agent agent, ref ConnectionLength conLength)
		{
			return (EnterLength >= agent.Length //car fit 
			        || EnterLength >= conLength.Length); //car is larger than the road & the road is empty
		}

		public bool CouldAgentPartiallyEnter()
		{
			return EnterLength > 0f;
		}

		public float NewAgentCoord(ref ConnectionLength conLength)
		{
			return conLength.Length - EnterLength;
		}

		public void AcceptAgentFully(ref Agent agent)
		{
			//this allow a connection to accept an agent longer than its own length
			EnterLength = math.max(0f, EnterLength - agent.Length);
		}

		public int FramesToEnter(ref Connection connection)
		{
			return (int) math.ceil(EnterLength / connection.Speed);
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
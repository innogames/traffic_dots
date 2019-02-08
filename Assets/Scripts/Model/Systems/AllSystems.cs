using Model.Components;
using Unity.Entities;

namespace Model.Systems
{
	public class OccupySystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
//			throw new System.NotImplementedException();
			
			//all Occupant without OccupantState
			//dictionary? occupant to a node
		}

		public bool IsOccupied(Node node)
		{
			return true;
		}

		public Entity GetOccupant(Node node)
		{
			return Entity.Null;
		}
	}

	public class HexaGridSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
			//get all HexaGrid
			//generate HexaNode
			//generate Occupant
			//remove HexaGrid
			
			//periodically generate Occupant & Mover
		}

		public bool IsConnected(HexaNode a, HexaNode b)
		{
			return true;
		}
	}

	public class PathSystem : ComponentSystem
	{		
		public struct Zone : ISystemStateComponentData
		{
			public int ID;
		}

		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
			
			//get all node without zone
			//get all static occupant (without Mover)
			//assign a zone to all these node
			//compute path between all nodes in a zone
			
			//all movers move to next tile if empty!
			//mover wait, if has higher priority
			//mover look for other adjacents, passing lower priority movers
		}
	}
}
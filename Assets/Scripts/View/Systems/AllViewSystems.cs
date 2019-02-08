using Unity.Entities;

namespace View.Systems
{
	public class PositioningSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
			//get all mover without Position, add Position
			//get all Mover & Position, update Position

			//get all Occupant, without Mover and Position, add Position
		}
	}
}
using Unity.Entities;

namespace Model.Systems
{
	/// <summary>
	///	a connection:
	/// - transform entrance
	/// - transform exit
	/// - visualize: spline interpolation for the car
	/// 
	/// road segment: is one connection
	///
	/// lane merge: is two connections from two lanes to one
	///
	/// lane split: is two connections from one lane to two: path finding will decide
	///
	/// four-way intersection: each lane has three connections to three lanes on other roads
	///
	/// full traffic:
	/// - a connection has a capacity on how many car
	/// - when full, the connection is blocked
	/// - it's a queue? implemented with buffer
	/// - whoever arrive register with the segment to enter
	/// - the segment will pull in car from the queue when it becomes empty!
	/// </summary>
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RoadVisualizerSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
		}
	}
}
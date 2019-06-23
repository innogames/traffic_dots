using Unity.Entities;

namespace Model.Systems
{
	public struct TimerState : ISystemStateComponentData
	{
		public int CountDown;
	}
}
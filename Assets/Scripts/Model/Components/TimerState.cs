using Unity.Entities;

namespace Model.Components
{
	public struct TimerState : IComponentData
	{
		public int CountDown;
	}
}
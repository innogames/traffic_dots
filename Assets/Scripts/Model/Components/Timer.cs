using Unity.Entities;

namespace Model.Components
{
	public struct Timer : IComponentData
	{
		public int Frames;
		public TimerType TimerType;
	}
}
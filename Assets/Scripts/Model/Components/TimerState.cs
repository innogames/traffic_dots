using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct TimerState : IComponentData
	{
		public int CountDown;
	}
}
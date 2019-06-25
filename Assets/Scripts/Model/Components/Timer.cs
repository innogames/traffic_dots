using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Timer : IComponentData
	{
		public int Frames;
		public TimerType TimerType;

		public void ChangeToEveryFrame(ref TimerState state)
		{
			TimerType = TimerType.EveryFrame;
			
			//because we only process when Countdown == 0
			//it will stuck in every frame mode forever
			//even after switching back to Ticking!
			//so we reset the countdown here!
			state.CountDown = Frames;
		}
	}
}
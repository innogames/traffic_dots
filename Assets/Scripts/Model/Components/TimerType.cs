namespace Model.Components
{
	public enum TimerType
	{
		Ticking,
		EveryFrame, //use this in order to switch back to normal interval again!
		Freezing,
	}
}
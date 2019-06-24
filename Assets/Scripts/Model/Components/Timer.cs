using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Timer : IComponentData
	{
		public int Frames;
		public TimerType TimerType;
	}
}
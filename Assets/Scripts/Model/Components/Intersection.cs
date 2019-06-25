using System;
using Unity.Entities;

namespace Model.Components
{
	[Serializable]
	public struct Intersection : IComponentData
	{
		public int Phase;
		public IntersectionPhaseType PhaseType;
	}
}
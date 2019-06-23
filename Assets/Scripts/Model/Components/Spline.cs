using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Model.Components
{
	[Serializable]
	public struct Spline : IComponentData
	{
		public float3 a;
		public float3 b;
		public float3 c;
		public float3 d;
	}
}
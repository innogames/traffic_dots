using UnityEngine;

namespace Config
{
	public static class ConfigConstants
	{
		public static readonly Vector3 OffsetZ = Vector3.up * 0.1f; //to avoid z fight
		private const float Epsilon = 1.0f;

		public static bool Connected(Transform a, Transform b)
		{
			return (a.position - b.position).sqrMagnitude < Epsilon;
		}
	}
}
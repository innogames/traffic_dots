using UnityEngine;

namespace Config
{
	public static class ConfigConstants
	{
		private const float Epsilon = 1.0f;

		public static bool Connected(Transform a, Transform b)
		{
			return (a.position - b.position).sqrMagnitude < Epsilon;
		}
	}
}
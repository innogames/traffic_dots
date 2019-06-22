using UnityEngine;

namespace Config
{
	[CreateAssetMenu(fileName = "CityConfig", menuName = "City Config")]
	public class CityConfig : ScriptableObject
	{
		public RoadSegment[] Segments;
	}
}
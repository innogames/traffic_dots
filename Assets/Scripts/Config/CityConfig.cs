using Config.Proxy;
using UnityEngine;

namespace Config
{
	[CreateAssetMenu(fileName = "CityConfig", menuName = "City Config")]
	public class CityConfig : ScriptableObject
	{
		public RoadSegment[] Segments;
		public AgentProxy[] Vehicles;
		public Mesh ConeMesh;
		public float ConnectionBaseSpeed = 12f;
		public int TargetFramerate = 60;
	}
}
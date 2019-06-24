using Unity.Entities;
using UnityEngine;

namespace Config
{
	public class AgentSpawnerConfig : MonoBehaviour
	{
		public int AgentIndex;
		public Connection SpawnLocation;
		public Connection TargetLocation;
		public int Interval;
	}
}
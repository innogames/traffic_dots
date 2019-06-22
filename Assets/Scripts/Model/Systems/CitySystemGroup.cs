using Unity.Entities;

namespace Model.Systems
{
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
	public class CitySystemGroup : ComponentSystemGroup
	{		
	}
}
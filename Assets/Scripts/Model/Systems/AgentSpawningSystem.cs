using Model.Components;
using Unity.Entities;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[UpdateAfter(typeof(TimerBufferSystem))]
	public class AgentSpawningSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, AgentSpawner spawner, ref TimerState timer, 
				ref ConnectionLocation location, ref ConnectionDestination destination) =>
			{
				if (timer.CountDown == 0)
				{
					//TODO check if location is empty!
					var agent = EntityManager.Instantiate(spawner.Agent);
					EntityManager.AddComponentData(agent, location);
					EntityManager.AddComponentData(agent, destination);					
				}
			});
		}
	}
}
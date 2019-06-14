using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Model.Systems
{
	public class AgentVisualizer : ComponentSystem
	{
		protected override void OnUpdate()
		{
			long curTimestamp = 0; //TODO get the current timestamp here
			Entities.ForEach((ref Agent agent, ref AgentAttachment attachment, ref Translation translation) =>
			{
				float t = (curTimestamp - attachment.StartTimestamp) * agent.Speed / attachment.CostDistance;
				translation.Value = math.lerp(attachment.StartPosition, attachment.EndPosition, t);
			});
		}
	}
}
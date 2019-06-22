using Unity.Entities;
using UnityEngine;

namespace Model.Systems
{
	[UpdateInGroup(typeof(CitySystemGroup))]
	[ExecuteAlways]
	public class PathCacheCommandBufferSystem : EntityCommandBufferSystem
	{
	}
}
using System.Collections;
using System.Linq;
using Model.Systems;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Tests
{
	public class NetworkSystemTest: ECSTestsFixture
	{
		[UnityTest]
		public IEnumerator NodeCreation()
		{
			var nodeEnt = m_Manager.CreateEntity(typeof(Node));
			m_Manager.SetComponentData(nodeEnt, new Node
			{
				Position = new float3(0, 0, 0),
			});
			yield return null;
			var networkSystem = World.Systems.OfType<NetworkSystem>();
			Assert.IsTrue(networkSystem != null);
		}
	}
}
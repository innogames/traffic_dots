using System.Linq;
using Model.Systems.City;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Tests
{
	public class CitySystemTest : ECSTestsFixture
	{
		private Entity AddNode(float3 position)
		{
			var node = m_Manager.CreateEntity(typeof(Node));
			m_Manager.SetComponentData(node, new Node
			{
				Position = position,
			});
			return node;
		}

		private Entity AddConnection(Entity startNode, Entity endNode)
		{
			var connection = m_Manager.CreateEntity(typeof(Connection));
			m_Manager.SetComponentData(connection, new Connection
			{
				StartNode = startNode,
				EndNode = endNode,
			});
			return connection;
		}
		
		[Test]
		public void NodeCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var citySystem = World.CreateSystem<CitySystem>();
			citySystem.Update();
			Assert.IsTrue(m_Manager.HasComponent<NodeData>(nodeA));
			Assert.IsTrue(m_Manager.HasComponent<NodeData>(nodeB));
		}
		
		[Test]
		public void ConnectionCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var citySystem = World.CreateSystem<CitySystem>();
			citySystem.Update();
			var road = AddConnection(nodeA, nodeB);
			citySystem.Update();

			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				var networkEntity = entities.FirstOrDefault(entity => m_Manager.HasComponent<NetworkData>(entity));
				var networkData = m_Manager.GetSharedComponentData<NetworkData>(networkEntity);
			}

			Assert.IsTrue(m_Manager.GetComponentData<NodeData>(nodeA).Network != Entity.Null);
			Assert.IsTrue(m_Manager.GetComponentData<NodeData>(nodeB).Network != Entity.Null);
		}
		
		[Test]
		public void TwoConnectionCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var citySystem = World.CreateSystem<CitySystem>();
			citySystem.Update();
			var roadAB = AddConnection(nodeA, nodeB);
			citySystem.Update();
			var roadBC = AddConnection(nodeB, nodeC);
			citySystem.Update();
			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				var networkEntity = entities.FirstOrDefault(entity => m_Manager.HasComponent<NetworkData>(entity));
				var networkData = m_Manager.GetSharedComponentData<NetworkData>(networkEntity);
				Assert.IsTrue(networkData.NextConnection(nodeA, nodeC) == roadAB);
			}
		}
	}
}
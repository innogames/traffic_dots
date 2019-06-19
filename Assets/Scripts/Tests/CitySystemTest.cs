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
				Cost = 1f,
			});
			return connection;
		}
		
		private Entity AddAgent(Entity node, Entity destination)
		{
			var agent = m_Manager.CreateEntity(typeof(Agent), typeof(NodeAttachment),typeof(PathIntent));
			m_Manager.SetComponentData(agent, new Agent
			{
				Speed = 1f,
			});
			m_Manager.SetComponentData(agent, new NodeAttachment
			{
				Node = node,
			});
			m_Manager.SetComponentData(agent, new PathIntent
			{
				EndNode = destination,
			});
			
			return agent;
		}
		
		[Test]
		public void NodeCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));

			UpdateSystems();

			Assert.IsTrue(m_Manager.HasComponent<NodeData>(nodeA));
			Assert.IsTrue(m_Manager.HasComponent<NodeData>(nodeB));
		}

		private void UpdateSystems()
		{
			World.GetOrCreateSystem<CityNodeSystem>().Update();			
			m_Manager.CompleteAllJobs();
			World.GetOrCreateSystem<NodeDataCommandBufferSystem>().Update();
			World.GetOrCreateSystem<CityConnectionSystem>().Update();
			m_Manager.CompleteAllJobs();
			World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
		}

		[Test]
		public void ConnectionCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var road = AddConnection(nodeA, nodeB);
			
			UpdateSystems();

			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				var networkEntity = entities.FirstOrDefault(entity => m_Manager.HasComponent<NetworkSharedData>(entity));
				var networkData = m_Manager.GetSharedComponentData<NetworkSharedData>(networkEntity);
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
			var citySystem = World.CreateSystem<CityConnectionSystem>();
			citySystem.Update();
			var roadAB = AddConnection(nodeA, nodeB);
			citySystem.Update();
			var roadBC = AddConnection(nodeB, nodeC);
			citySystem.Update();
			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				var networkEntity = entities.FirstOrDefault(entity => m_Manager.HasComponent<NetworkSharedData>(entity));
				var networkData = m_Manager.GetSharedComponentData<NetworkSharedData>(networkEntity);
				Assert.IsTrue(networkData.NextConnection(nodeA, nodeC) == roadAB);
				Assert.IsTrue(networkData.Distance(nodeA, nodeC) <= 2.0f);
			}
		}
		
		[Test]
		public void AgentWithPathIntent()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var citySystem = World.CreateSystem<CityConnectionSystem>();
			citySystem.Update();
			var roadAB = AddConnection(nodeA, nodeB);
			citySystem.Update();
			var roadBC = AddConnection(nodeB, nodeC);
			citySystem.Update();

			var agent = AddAgent(nodeA, nodeC);
			citySystem.Update();
			
			
			
			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				var networkEntity = entities.FirstOrDefault(entity => m_Manager.HasComponent<NetworkSharedData>(entity));
				var networkData = m_Manager.GetSharedComponentData<NetworkSharedData>(networkEntity);
				Assert.IsTrue(networkData.NextConnection(nodeA, nodeC) == roadAB);
				Assert.IsTrue(networkData.Distance(nodeA, nodeC) <= 2.0f);
			}
		}
	}
}
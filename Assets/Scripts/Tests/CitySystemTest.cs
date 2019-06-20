using System.Collections.Generic;
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
			var node = m_Manager.CreateEntity(typeof(Node), typeof(NextBuffer));
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
			World.GetOrCreateSystem<CityAddConnectionSeqSystem>().Update();
//			World.GetOrCreateSystem<NetworkCreationSystem>().Update();
			
			World.GetOrCreateSystem<PathSystem>().Update();
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
				var networkEntity = entities.First(entity => m_Manager.HasComponent<Network>(entity));
				Assert.IsTrue(m_Manager.GetBuffer<NetAdjust>(networkEntity)[0].Connection == road);
			}

			Assert.IsTrue(m_Manager.HasComponent<NetworkGroup>(road));
		}
		
		[Test]
		public void TwoConnectionCreation()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);
			
			UpdateSystems();
			
			using (var entities = m_Manager.GetAllEntities(Allocator.Temp))
			{
				Assert.IsTrue(entities.Count(entity => m_Manager.HasComponent<Network>(entity)) == 1);
				var networkEntity = entities.First(entity => m_Manager.HasComponent<Network>(entity));
				Assert.AreEqual(2, m_Manager.GetBuffer<NetAdjust>(networkEntity).Length);
			}
			
			Assert.IsTrue(m_Manager.GetSharedComponentData<NetworkGroup>(roadAB).NetworkId == 
			              m_Manager.GetSharedComponentData<NetworkGroup>(roadBC).NetworkId);

			var nextA = m_Manager.GetBuffer<NextBuffer>(nodeA);
			var indexB = m_Manager.GetComponentData<IndexInNetwork>(nodeB);
			Assert.IsTrue(nextA[indexB.Index].Connection == roadAB);
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
using System.Linq;
using Model.Components;
using Model.Systems;
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

		private Entity AddAgent(Entity connection, Entity endNode)
		{
			var agent = m_Manager.CreateEntity(typeof(Agent), typeof(PathIntent));
			m_Manager.SetComponentData(agent, new Agent
			{
				Connection = connection,
				Speed = 1.0f,
			});
			m_Manager.SetComponentData(agent, new PathIntent
			{
				EndNode = endNode,
			});
			return agent;
		}

		private void UpdateSystems()
		{
			World.GetOrCreateSystem<CityAddConnectionSeqSystem>().Update();

			World.GetOrCreateSystem<NetworkCreationSystem>().Update();
			m_Manager.CompleteAllJobs();
			World.GetOrCreateSystem<PathCacheCommandBufferSystem>().Update();
			
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
			var indexC = m_Manager.GetComponentData<IndexInNetwork>(nodeC);
			Assert.IsTrue(nextA[indexC.Index].Connection == roadAB);

			var nextC = m_Manager.GetBuffer<NextBuffer>(nodeC);			
			var indexA = m_Manager.GetComponentData<IndexInNetwork>(nodeA);
			Assert.IsTrue(nextC[indexA.Index].Connection == roadBC);
		}
		
		[Test]
		public void AgentWithPathIntent()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);

			var agent = AddAgent(roadAB, nodeC);
			
			UpdateSystems();
			Assert.IsTrue(m_Manager.GetComponentData<Agent>(agent).Connection == roadBC);
		}
	}
}
using System.Collections.Generic;
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
				Speed = 1f,
			});
			return connection;
		}

		private Entity AddAgent(Entity connectionLocation, Entity connectionTarget)
		{
			var agent = m_Manager.CreateEntity(typeof(Agent),
				typeof(ConnectionLocation),
				typeof(ConnectionDestination));
			m_Manager.SetComponentData(agent, new ConnectionLocation
			{
				Connection = connectionLocation,
				Slot = 0,
			});
			m_Manager.SetComponentData(agent, new ConnectionDestination
			{
				Connection = connectionTarget,
				Slot = 0,
			});
			return agent;
		}

		private void UpdateSystems()
		{
			World.GetOrCreateSystem<TimerSystem>().Update();
			World.GetOrCreateSystem<TimerBufferSystem>().Update();
			
			World.GetOrCreateSystem<AgentSpawningSystem>().Update();

			World.GetOrCreateSystem<CityAddConnectionSeqSystem>().Update();
			
			World.GetOrCreateSystem<NetworkCreationSystem>().Update();
			World.GetOrCreateSystem<PathCacheCommandBufferSystem>().Update();
			
			World.GetOrCreateSystem<PathSystem>().Update();
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
				Assert.AreEqual(1, entities.Count(entity => m_Manager.HasComponent<Network>(entity)));
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
				Assert.AreEqual(1, entities.Count(entity => m_Manager.HasComponent<Network>(entity)));
			}

			Assert.IsTrue(m_Manager.GetSharedComponentData<NetworkGroup>(roadAB).NetworkId ==
			              m_Manager.GetSharedComponentData<NetworkGroup>(roadBC).NetworkId);

			var nextA = m_Manager.GetBuffer<NextBuffer>(nodeA);
			var indexC = m_Manager.GetComponentData<IndexInNetwork>(nodeC);
			Assert.IsTrue(nextA[indexC.Index].Connection == roadAB);

			var nextC = m_Manager.GetBuffer<NextBuffer>(nodeC);
			var indexA = m_Manager.GetComponentData<IndexInNetwork>(nodeA);
			Assert.IsTrue(nextC[indexA.Index].Connection == Entity.Null);
		}

		[Test]
		public void AgentWithPathIntent()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);

			var agent = AddAgent(roadAB, roadBC);

			UpdateSystems();
			Assert.IsTrue(m_Manager.GetComponentData<ConnectionLocation>(agent).Connection == roadBC);
		}

		const int size = 4;
		private const int sizeSqr = size * size;

		private int Coord(int x, int y)
		{
			return x * size + y;
		}

		[Test]
		public void GridCreation()
		{
			var nodes = new Entity[sizeSqr];
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					nodes[Coord(x, y)] = AddNode(new float3(x, y, 0f));
				}
			}

			var horiCon = new Entity[sizeSqr];
			var vertiCon = new Entity[sizeSqr];

			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					var xy = Coord(x, y);
					if (x + 1 < size)
					{
						var xy_right = Coord(x + 1, y);
						horiCon[xy] = AddConnection(nodes[xy], nodes[xy_right]);
					}

					if (y + 1 < size)
					{
						var xy_down = Coord(x, y + 1);
						vertiCon[xy] = AddConnection(nodes[xy], nodes[xy_down]);
					}
				}
			}

			var endCon = horiCon[Coord(size - 2, size - 2)];
			var agent = AddAgent(horiCon[Coord(0, 0)], endCon);

			UpdateSystems();
			using (var entities = m_Manager.GetAllEntities())
			{
				Assert.AreEqual(1, entities.Count(m_Manager.HasComponent<Network>));				
			}

			for (int i = 0; i < size * 2; i++)
			{
				UpdateSystems();
			}
			var agent_end_con = m_Manager.GetComponentData<ConnectionLocation>(agent).Connection;
			Assert.AreEqual(endCon, agent_end_con);
		}
	}
}
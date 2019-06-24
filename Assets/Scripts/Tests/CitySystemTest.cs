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
			var connection = m_Manager.CreateEntity(typeof(Connection),
				typeof(Spline), typeof(EntitySlot));
			m_Manager.SetComponentData(connection, new Connection
			{
				StartNode = startNode,
				EndNode = endNode,
				Speed = 1f,
				Length = 2f,
			});
			m_Manager.SetComponentData(connection, new Spline
			{
				a = m_Manager.GetComponentData<Node>(startNode).Position,
				//TODO compute correct spline here!
				b = float3.zero,
				c = float3.zero,
				d = m_Manager.GetComponentData<Node>(endNode).Position,				
			});
			m_Manager.SetComponentData(connection, new EntitySlot
			{
				SlotCount = 2,
			});
			return connection;
		}

		private Entity AddAgent(Entity connectionLocation, Entity connectionTarget)
		{
			var agent = m_Manager.CreateEntity(typeof(Agent),
				typeof(ConnectionLocation),
				typeof(ConnectionDestination),
				typeof(Timer));
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
			m_Manager.SetComponentData(agent, new Timer
			{
				Frames = 1, //act every frame!
			});
			return agent;
		}
		
		private Entity AddVarioAgent(Entity connectionCoord, float startCoord, Entity connectionTarget, float length)
		{
			var agent = m_Manager.CreateEntity(typeof(Agent),
				typeof(ConnectionCoord),
				typeof(ConnectionTarget),
				typeof(Timer));
			m_Manager.SetComponentData(agent, new Agent
			{
				Length = length,
			});
			m_Manager.SetComponentData(agent, new ConnectionCoord
			{
				Connection = connectionCoord,
				Coord = startCoord,
			});
			m_Manager.SetComponentData(agent, new ConnectionTarget
			{
				Connection = connectionTarget,
			});
			m_Manager.SetComponentData(agent, new Timer
			{
				Frames = 1, //act every frame!
			});
			return agent;
		}
		
		private Entity CreateAgentPrefab(float length)
		{
			var agentPrefab = m_Manager.CreateEntity(typeof(Agent),
				typeof(ConnectionCoord),
				typeof(ConnectionTarget),
				typeof(Timer),
				typeof(TimerState),
				typeof(Disabled));
			m_Manager.SetComponentData(agentPrefab, new Agent
			{
				Length = length,
			});
			return agentPrefab;
		}
		
		private Entity AddAgentSpanwer(Entity agentPrefab, Entity spawnConnection, Entity targetConnection, int interval)
		{
			var spawner = m_Manager.CreateEntity(typeof(AgentSpawner),
				typeof(ConnectionCoord),
				typeof(ConnectionTarget),
				typeof(Timer));
			m_Manager.SetSharedComponentData(spawner, new AgentSpawner
			{
				Agent = agentPrefab,				
			});
			m_Manager.SetComponentData(spawner, new ConnectionCoord
			{
				Connection = spawnConnection,
				Coord = 0, //this value is not used by AgentSpawningSystem
			});
			m_Manager.SetComponentData(spawner, new ConnectionTarget
			{
				Connection = targetConnection,
			});
			m_Manager.SetComponentData(spawner, new Timer()
			{
				Frames = interval,
				TimerType = TimerType.Ticking,
			});
			m_Manager.RemoveComponent<Disabled>(spawner);
			return spawner;
		}

		private void UpdateSystems()
		{
			World.GetOrCreateSystem<SplineSystem>().Update();
			World.GetOrCreateSystem<EntitySlotSystem>().Update();
			World.GetOrCreateSystem<AgentQueuePrepSystem>().Update();
			
			World.GetOrCreateSystem<TimerSystem>().Update();
			World.GetOrCreateSystem<TimerBufferSystem>().Update();

			World.GetOrCreateSystem<CityAddConnectionSeqSystem>().Update();
			
			World.GetOrCreateSystem<NetworkCreationSystem>().Update();
			World.GetOrCreateSystem<PathCacheCommandBufferSystem>().Update();
			
			World.GetOrCreateSystem<PathSystem>().Update();

			World.GetOrCreateSystem<AgentSpawningSystem>().Update();
			World.GetOrCreateSystem<AgentQueueSystem>().Update();
			
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
			Assert.AreEqual(2, m_Manager.GetBuffer<EntitySlotBuffer>(road).Length);
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
		public void AgentInPathSystem()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);

			var agent = AddAgent(roadAB, roadBC);

			UpdateSystems();
			var bufferAB = m_Manager.GetBuffer<EntitySlotBuffer>(roadAB);
			var bufferBC = m_Manager.GetBuffer<EntitySlotBuffer>(roadBC);
			Assert.AreEqual(2, bufferAB.Length, "road buffer");
			Assert.AreEqual(2, bufferBC.Length, "road buffer");
			Assert.AreEqual(agent, bufferAB[1].Agent, "agent location");

//			var timer = m_Manager.GetComponentData<Timer>(agent).Frames;
//			var timerState = m_Manager.GetComponentData<TimerState>(agent).CountDown;
//
//			var ab0 = bufferAB[0].Agent;
//			var ab1 = bufferAB[1].Agent;
//			var bc0 = bufferBC[0].Agent;
//			var bc1 = bufferBC[1].Agent;

//			var agentLocation = m_Manager.GetComponentData<ConnectionLocation>(agent);
			
			Assert.AreEqual(1, m_Manager.GetComponentData<TimerState>(agent).CountDown, "timer state");
			Assert.AreEqual(1, m_Manager.GetComponentData<ConnectionLocation>(agent).Slot, "location slot");
			
			UpdateSystems();
			Assert.AreEqual(roadBC, m_Manager.GetComponentData<ConnectionLocation>(agent).Connection, "reach target");
		}

		[Test]
		public void AgentInAgentQueueSystem()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);

			var agentA = AddVarioAgent(roadAB, 0f, roadBC, 1f);
			var agentB = AddVarioAgent(roadAB, 1f, roadBC, 1f);

			UpdateSystems();
//			var timer = m_Manager.GetComponentData<Timer>(agent).Frames;
//			var timerState = m_Manager.GetComponentData<TimerState>(agent).CountDown;
//

//			var agentLocation = m_Manager.GetComponentData<ConnectionLocation>(agent);
			
			Assert.AreEqual(2, m_Manager.GetComponentData<TimerState>(agentA).CountDown, "timer state");
			Assert.AreEqual(0, m_Manager.GetComponentData<ConnectionCoord>(agentA).Coord, "agent A coord");
			Assert.AreEqual(1, m_Manager.GetComponentData<ConnectionCoord>(agentB).Coord, "agent B coord");
			
			UpdateSystems();
			Assert.AreEqual(roadBC, m_Manager.GetComponentData<ConnectionCoord>(agentA).Connection, "reach target");
		}
		
		[Test]
		public void AgentSpawning()
		{
			var nodeA = AddNode(new float3(0, 0, 0));
			var nodeB = AddNode(new float3(1, 0, 0));
			var nodeC = AddNode(new float3(1, 1, 0));
			var roadAB = AddConnection(nodeA, nodeB);
			var roadBC = AddConnection(nodeB, nodeC);

			var agentPrefab = CreateAgentPrefab(1f);
			AddAgentSpanwer(agentPrefab, roadAB, roadBC, 1);
			
			UpdateSystems();

			var buffer = m_Manager.GetBuffer<AgentQueueBuffer>(roadAB);
			var agentA = buffer[0].Agent;
			var agentCoord = m_Manager.GetComponentData<ConnectionCoord>(agentA);
			Assert.AreEqual(roadAB, agentCoord.Connection, "agent A spawned correct connection!");
			Assert.AreEqual(0, agentCoord.Coord, "agent A spawned correct position");

			UpdateSystems();
			buffer = m_Manager.GetBuffer<AgentQueueBuffer>(roadAB);
			var agentB = buffer[1].Agent;
			var agentBCoord = m_Manager.GetComponentData<ConnectionCoord>(agentB);
			
			Assert.AreEqual(roadAB, agentBCoord.Connection, "agent B spawned correct connection!");
			Assert.AreEqual(1, agentBCoord.Coord, "agent B spawned correct position");
			
			UpdateSystems();
			Assert.AreEqual(roadBC, m_Manager.GetComponentData<ConnectionCoord>(agentA).Connection, "agent A reach target");

			UpdateSystems();
			Assert.AreEqual(roadBC, m_Manager.GetComponentData<ConnectionCoord>(agentB).Connection, "agent B reach target");
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
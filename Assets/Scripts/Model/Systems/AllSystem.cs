namespace Model.Systems
{
	public class AllSystem
	{
		//if divided district by using square
		//20x20 with no hierarchy = 400 nodes ==> 400x400 = 160000
		//20x20 full map divided to 4 districts
		//10x10 each district has 100 nodes (100 * 100 * 4 = 40000)
		//total divisive nodes: 39. 39 * 39 = 1521
		//total sum = 40000 + 1521 = 41521
		//we reduce to: 41521 / 160000 = 26%
		//that is a significant save, we don't have to optimize district division!
		//if the hierarchy save is good enough, we don't need the straight road save
		//and without straight road save, the lane switch can use the same system

		//if a road cut the division line?
		//choose one of its intersection as the exit (divisive node)

		//node has
		//- position
		//- dict<node, network> neighbours: key is neighbour, value is the network the connection belongs to
		//- neighbours.values.distinct: is a list of network with continuous depth, from the lowest depth
		//network: is a list of connections
		//- depth: how far it is from the highest network
		//- list<node> nodeIndexes
		//- cache: distances between all node indexes 
		//- - dist(x,y): shortest distance between node x and node y
		//- - if the network is the lowest: it's the defined distance
		//- - next(x,y): where the path from x to y go to
		
		//network 

		//1. path finding start from one nodeA to nodeB
		//2. check if nodeA.currentNetwork == nodeB.currentNetwork
		//3. if yes, path = nodeA.currentNetwork.cache.next[nodeA, nodeB]
		//4. if no, compute the lowest common network = commonNetwork
		//5. perform A star, use only upper connections until reaching common network
		//6. in the common network, use indirect connections to get to node B's network
		//7. continue A star, use only downward connections, until reaching node B!

		//add connection: nodeA, nodeB: always belongs to the lowest network
		//if two nodes are NOT at the same lowest network: elevate one of them
		//if two nodes belongs to the same lowest network
		//nodeA.update(nodeB, newDistance) & nodeB.update(nodeA, newDistance):
		//node.update(otherNode, newDistance)
		//- node.neighbours.Add(otherNode, node.lowestNetwork)
		//- for (var network in node.networks)
		//- - network.cache.dist(node, otherNode) = newDistance
		//- - network.cache.update(node, otherNode, newDistance)
		//- - - openList = {{node, otherNode}, {otherNode, node}}				
		//- - - while (var check = openList.pop())
		//- - - - check.key.neighbours.each(nei => update(next[nei, check.val], openList)
		
		
		///approach 3: City System: change to alley, road, highway
		///algorithm: find the closest exit to the higher network
		///e.g: from the alley, find the closest road, then the closest highway to the destination!
		///e.g: from the house, walk to the bus station, then to the train station, and so on!
		///cached info:
		///within the network: dist and next of all nodes
		///for each node: exit to the closest higher network node
		///e.g: house to intersection (how to prevent wrong one?), alley to road, road to highway
	
		///network separation?
		///if the shortest path between two nodes have to go through a higher node ==> they belong to two networks!
		///==> use coloring algorithm!
		///how to know before adding to a network? just append to an existing network, compute, then split!
		/// the new node is appended at the end, easy to split!
		///
		/// prevent wrong intersection from house: not allowing u-turn!
		/// so there is only one valid intersection from and to a house!
		/// for walking: can test 2x2 combination of start and end intersection!
		///
		/// NetworkData should be a SharedComponent on each node: to use shared component filter & cache locality!
		/// NetworkData could just have an ID for filter purpose!
		/// Dist and Next should be DynamicBuffer on each Node
		///
		/// Highway node should not belong to road network
		/// Road exit to high-way will first exit to the road node connected to highway (outer-node)
		/// These outer-node has exit directly to high-way node
		///
		/// update Dist and Next
		/// a new connection is created
		/// retrieve the network
		/// how to access dist[i][j]?
		/// NetworkData should link to another entity, for NetworkInternalData
		/// from Entity to index: indexInNetwork
		/// from index to Entity?
		///
		/// use Entity as index
		/// HashMap<Entity, float> dist; HashMap<Entity, Entity> next; per entity! NOT POSSIBLE!

		
		//NetworkData
		//NativeHashMap<EntityPair, Entity> Next
		//key: from & to Node
		//value: next connection!
			
		//add node:
		//+ Node
		//- NodeData
			
		//add connection: SEQUENTIAL
		//all connected connection share the same network
		//shared comp NetworkAssociation has Network entity pointer
		//create NetworkSharedData entities to store Dist and Next
		//add NetAdjust to NetworkSharedData
			
		//compute Dist & Next
		//+ NetworkSharedData
		//+ NetAdjust
		//compute Dist & Next & delete NetAdjust
			
		//(agent) PathIntent
		//if same network: create SameNetwork (shared)
		//if dif network: create HigherNetwork & LowerNetwork
			
		//in order to use Next, NodeToIndex must be presented! Must group Agent by Network!
			
		//SameNetwork: group by SameNetwork
			
		//new method of using ComponentDataFromEntity!
		//NativeHashMap<EntityPair, Entity> becomes
		//NextBuffers[fromNode][IndexInNetworks[toNode].Value].Connection == next connection!
		//Exits[fromNode] == exit node in the same network!
		//DirectExits[fromNode] == exit connection 

		
		//multi-lane & one direction road?
		//each lane has a number of slots as Buffer
		//first car: go into slot 1: need to know car number
		//second car: go into slot 2: need to know car number
		//first car exit: slot 1 vacate
		//second car: go into slot 1: need to know either:
		//- first car trigger second car
		//- the road needs to know which car is in 2nd? and 3rd, so on
		//each lane has a buffer, pointing to car, in a queue
		
		//one or two directions connection
		//one direction:
		//- easier path finding
		//- easier intersection modeling
		//two directions:
		//- less connection entity
		//- faster path finding?
		//- not possible to model one-direction road!
		
		//intersection?
		//has a number of "pipe" connecting lanes
		//intersection is a connection that car can't stay
		//when entering an intersection, look ahead to the next connection for empty slot
		//so, the road slot of the next connection is "pro-long" in "receiving" its car
		//"pipe" connects two connections!
		//"pipe" forms intersection, traffic light, lane merge, lane split & parking!
		
		
		//lane switching?
		//- the inner lane has higher speed limit
		//- outer lane can turn right
		//- other road turn into the outer lane
		//path finding will prioritize inner lane for straight car 
		//car does a final lane switch before turning!
		//only possible if each lane is modeled as a connection!
		//BUT that would not allow lane balancing, the outer lane will be vacated!
		
		//road has multi-lane
		//common buffer for all lanes
		//visualization will be offset
	}
}
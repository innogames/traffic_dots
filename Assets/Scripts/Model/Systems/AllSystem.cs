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
	}
}
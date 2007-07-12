using System;

namespace CellDotNet
{
	public class Graph
	{
		private Set<Node> _nodes = new Set<Node>();
		public Set<Node> Nodes
		{
			get { return _nodes; }
		}

		public Node NewNode()
		{
			Node n = new Node(this);
			_nodes.Add(n);
			return n;
		}

		public void AddEdge(Node from, Node to)
		{
			if (from.Graph != this || to.Graph != this)
				throw new ArgumentException("Nodes do not belong to the graph.");
			from.Succ.Add(to);
			to.Pred.Add(from);
		}

		public void RemoveEdge(Node from, Node to)
		{
			if (from.Graph != this || to.Graph != this)
				throw new ArgumentException("Nodes do not belong to the graph.");
			from.Succ.Remove(to);
			to.Pred.Remove(from);
		}
	}

	public class Node
	{
		private Set<Node> _succ = new Set<Node>();
		// NOTE: Succ og Pred returneres en referense til den interne representation,
		// så der skal laves en kopi inden der laves strukturelle ændringer på den returnerede liste.
		public Set<Node> Succ
		{
			get { return _succ; }
		}

		// Ikke nødvendig for at representere grafer, men hurtgere i forbindelse med at finde forgængeren
		private Set<Node> _pred = new Set<Node>();
		public Set<Node> Pred
		{
			get { return _pred; }
		}

		private Graph _graph;
		public Graph Graph
		{
			get { return _graph; }
		}

		public Node(Graph graph)
		{
			_graph = graph;
		}

		public Set<Node> Adj()
		{
			Set<Node> nodeSet = new Set<Node>();
			nodeSet.AddAll(_succ);
			nodeSet.AddAll(_pred);
			return nodeSet;
		}

		public int OutDegree()
		{
			return _succ.Count;
		}

		public int InDegree()
		{
			return _pred.Count;
		}

		public int Degree()
		{
			return Adj().Count;
		}

		public bool GoesTo(Node node)
		{
			return _succ.Contains(node);
		}

		public bool ComesFrom(Node node)
		{
			return _pred.Contains(node);
		}

		public bool Adj(Node node)
		{
			return GoesTo(node) || ComesFrom(node);
		}
	}
}

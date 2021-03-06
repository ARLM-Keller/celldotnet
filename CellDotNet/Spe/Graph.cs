// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace CellDotNet.Spe
{
	public class Graph
	{
		private Set<GraphNode> _nodes = new Set<GraphNode>();
		public Set<GraphNode> Nodes
		{
			get { return _nodes; }
		}

		private Set<GraphNode> _frozenNodes = new Set<GraphNode>();

		private bool isFrozen;

		virtual public GraphNode NewNode()
		{
			GraphNode n = new GraphNode(this);
			_nodes.Add(n);
			return n;
		}

		public void AddEdge(GraphNode from, GraphNode to)
		{
			if (from.Graph != this || to.Graph != this)
				throw new ArgumentException("Nodes do not belong to the graph.");

			if (isFrozen && (!_frozenNodes.Contains(from) || !_frozenNodes.Contains(to)))
				throw new ArgumentException();

			from.Succ.Add(to);
			to.Pred.Add(from);
		}

		public void RemoveEdge(GraphNode from, GraphNode to)
		{
			if (from.Graph != this || to.Graph != this)
				throw new ArgumentException("Nodes do not belong to the graph.");

			if (isFrozen && (!_frozenNodes.Contains(from) || !_frozenNodes.Contains(to)))
				throw new ArgumentException();

			from.Succ.Remove(to);
			to.Pred.Remove(from);
		}

		public void Freeze()
		{
			Utilities.Assert(!isFrozen, "!isFrozen");

			_frozenNodes.AddAll(_nodes);

			foreach (GraphNode node in _nodes)
				 node.Freeze();
		}

		public void RemoveNode(GraphNode graphNode)
		{
			if (graphNode.Graph != this)
				throw new ArgumentException();

			_nodes.Remove(graphNode);

			foreach (GraphNode succ in graphNode.Succ)
				RemoveEdge(graphNode, succ);

			foreach (GraphNode pred in graphNode.Pred)
				RemoveEdge(pred, graphNode);

			if (!isFrozen)
				graphNode.Graph = null;
		}

		public void AddFrozenNodeWithEdges(GraphNode graphNode)
		{
			if (!isFrozen)
				throw new InvalidOperationException("Graph not frozen.");

			if (graphNode.Graph != this )
				throw new ArgumentException();

			if (isFrozen && (!_frozenNodes.Contains(graphNode)))
				throw new ArgumentException();

			_nodes.Add(graphNode);

			foreach (GraphNode succ in graphNode.FrozenSucc)
				if (_nodes.Contains(succ))
					AddEdge(graphNode, succ);

			foreach (GraphNode pred in graphNode.FrozenPred)
				if (_nodes.Contains(pred))
					AddEdge(pred, graphNode);
		}
	}

	public class GraphNode
	{
		private Set<GraphNode> _succ = new Set<GraphNode>();
		// NOTE: Succ og Pred returneres en referense til den interne representation,
		// s� der skal laves en kopi inden der laves strukturelle �ndringer p� den returnerede liste.
		public Set<GraphNode> Succ
		{
			get { return _succ; }
		}

		// Ikke n�dvendig for at representere grafer, men hurtgere i forbindelse med at finde forg�ngeren
		private Set<GraphNode> _pred = new Set<GraphNode>();
		public Set<GraphNode> Pred
		{
			get { return _pred; }
		}

		private Set<GraphNode> _frozenPred = new Set<GraphNode>();
		public Set<GraphNode> FrozenPred
		{
			get { return _frozenPred; }
		}

		private Set<GraphNode> _frozenSucc = new Set<GraphNode>();
		public Set<GraphNode> FrozenSucc
		{
			get { return _frozenSucc; }
		}

		private bool isFrozen;

		private Graph _graph;
		public Graph Graph
		{
			get { return _graph; }
			set { _graph = value; }
		}

		public GraphNode(Graph graph)
		{
			_graph = graph;
		}

		public void Freeze()
		{
			if (isFrozen)
				throw new InvalidOperationException();

			_frozenPred.AddAll(_pred);
			_frozenSucc.AddAll(_succ);
		}

		public Set<GraphNode> Adj()
		{
			Set<GraphNode> nodeSet = new Set<GraphNode>();
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

		public bool GoesTo(GraphNode graphNode)
		{
			return _succ.Contains(graphNode);
		}

		public bool ComesFrom(GraphNode graphNode)
		{
			return _pred.Contains(graphNode);
		}

		public bool Adj(GraphNode graphNode)
		{
			return GoesTo(graphNode) || ComesFrom(graphNode);
		}
	}
}

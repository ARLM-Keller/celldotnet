using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class GraphTest
	{
		[Test]
		public void CreatGraphNodeTest()
		{
			Graph g = new Graph();
			GraphNode n = g.NewNode();
			g.Nodes.Contains(n);
		}

		[Test, ExpectedException(typeof(ArgumentException))]
		public void Graph()
		{
			Graph g1 = new Graph();
			Graph g2 = new Graph();
			GraphNode n1 = new GraphNode(g1);
			GraphNode n2 = new GraphNode(g2);

			g1.AddEdge(n1, n2);
		}

		//TODO flere test
	}
}

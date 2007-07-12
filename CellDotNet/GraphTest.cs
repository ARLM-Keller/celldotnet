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
			Node n = g.NewNode();
			g.Nodes.Contains(n);
		}

		[Test, ExpectedException(typeof(ArgumentException))]
		public void Graph()
		{
			Graph g1 = new Graph();
			Graph g2 = new Graph();
			Node n1 = new Node(g1);
			Node n2 = new Node(g2);

			g1.AddEdge(n1, n2);
		}

		//TODO flere test
	}
}

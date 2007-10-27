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

#if UNITTEST

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

#endif
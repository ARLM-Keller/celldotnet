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

using System.Collections.Generic;

namespace CellDotNet.Spe
{
	public class FlowGraph : Graph
	{
		private Dictionary<GraphNode, VirtualRegister> defs = new Dictionary<GraphNode, VirtualRegister>();
		private Dictionary<GraphNode, List<VirtualRegister>> uses = new Dictionary<GraphNode, List<VirtualRegister>>();

		private Dictionary<GraphNode, bool> isMoves = new Dictionary<GraphNode, bool>();

		public GraphNode NewNode(VirtualRegister def, List<VirtualRegister> use, bool isMove)
		{
			GraphNode graphNode = NewNode();
			defs[graphNode] = def;
			uses[graphNode] = use;
			isMoves[graphNode] = isMove;
			return graphNode;
		}

		public VirtualRegister Def(GraphNode graphNode)
		{
			return defs[graphNode];
		}

		public List<VirtualRegister> Use(GraphNode graphNode)
		{
			return uses[graphNode];
		}

		public bool IsMove(GraphNode graphNode)
		{
			return isMoves[graphNode];
		}
	}
}

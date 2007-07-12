using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	public class FlowGraph : Graph
	{
		private Dictionary<Node, VirtualRegister> defs = new Dictionary<Node, VirtualRegister>();
		private Dictionary<Node, List<VirtualRegister>> uses = new Dictionary<Node, List<VirtualRegister>>();

		private Dictionary<Node, bool> isMoves = new Dictionary<Node, bool>();

		public Node NewNode(VirtualRegister def, List<VirtualRegister> use, bool isMove)
		{
			Node node = NewNode();
			defs[node] = def;
			uses[node] = use;
			isMoves[node] = isMove;
			return node;
		}

		public VirtualRegister def(Node node)
		{
			return defs[node];
		}

		public List<VirtualRegister> use(Node node)
		{
			return uses[node];
		}

		public bool IsMove(Node node)
		{
			return isMoves[node];
		}


	}
}

using System.Collections.Generic;

namespace CellDotNet
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

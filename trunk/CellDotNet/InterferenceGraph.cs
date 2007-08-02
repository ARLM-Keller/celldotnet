using System;
using System.Collections.Generic;

namespace CellDotNet
{
	public class InterferenceGraph : Graph
	{
		private Dictionary<GraphNode, VirtualRegister> nodeVrDic = new Dictionary<GraphNode, VirtualRegister>();

		public GraphNode NewNode(VirtualRegister vr)
		{
			GraphNode graphNode = base.NewNode();
			nodeVrDic[graphNode] = vr;
			return graphNode;
		}

		override public GraphNode NewNode()
		{
			// Knuder i interferens grafer bør kun laves med NewNode(VirtualRegister).
			throw new Exception();
		}

		public VirtualRegister getVR(GraphNode graphNode)
		{
			if (!Nodes.Contains(graphNode))
				throw new ArgumentException("GraphNode do not belong to the graph.");
			return nodeVrDic[graphNode];
		}

	}
}

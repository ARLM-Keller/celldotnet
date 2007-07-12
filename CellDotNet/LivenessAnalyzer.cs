using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class LivenessAnalyzer
	{
		public static Graph CreatFlowGraph(List<SpuBasicBlock> basicBlocks)
		{
			Dictionary<SpuBasicBlock, Node> jumpTargets = new Dictionary<SpuBasicBlock, Node>();
			Dictionary<SpuBasicBlock, LinkedList<Node>> jumpSources = new Dictionary<SpuBasicBlock, LinkedList<Node>>();

			FlowGraph flowGraph = new FlowGraph();

			foreach(SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction spuinst = bb.Head;
				Node node = flowGraph.NewNode(spuinst.Def, spuinst.Use, false); //TODO isMove skal sættes!
				jumpTargets[bb] = node;

				if (spuinst.JumpTarget != null)
					jumpSources[bb].AddLast(node);

				while(spuinst.Next != null)
				{
					spuinst = spuinst.Next;
					node = flowGraph.NewNode(spuinst.Def, spuinst.Use, false); //TODO isMove skal sættes!
					if (spuinst.JumpTarget != null)
						jumpSources[bb].AddLast(node);
				}
			}

			foreach (SpuBasicBlock bb in jumpTargets.Keys)
			{
				Node targetNode = jumpTargets[bb];
				foreach (Node sourceNode in jumpSources[bb])
				{
					flowGraph.AddEdge(sourceNode, targetNode );
				}
			}

			return flowGraph;
		}

		public static Graph CreatInterferenceGraph(FlowGraph flowGtaph)
		{
			
		}
	}
}

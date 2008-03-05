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
	static class LivenessAnalyzer
	{
		public static Graph CreatFlowGraph(List<SpuBasicBlock> basicBlocks)
		{
			//TODO mangler indsættelse af kanter for normanle ikke jump instruktioner.

			Dictionary<SpuBasicBlock, GraphNode> jumpTargets = new Dictionary<SpuBasicBlock, GraphNode>();
			Dictionary<SpuBasicBlock, LinkedList<GraphNode>> jumpSources = new Dictionary<SpuBasicBlock, LinkedList<GraphNode>>();

			FlowGraph flowGraph = new FlowGraph();

			foreach(SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction spuinst = bb.Head;
				GraphNode graphNode = flowGraph.NewNode(spuinst.Def, spuinst.Use, false); //TODO isMove skal sættes!
				jumpTargets[bb] = graphNode;

				if (spuinst.JumpTarget != null)
					jumpSources[bb].AddLast(graphNode);

				while(spuinst.Next != null)
				{
					spuinst = spuinst.Next;
					graphNode = flowGraph.NewNode(spuinst.Def, spuinst.Use, false); //TODO isMove skal sættes!
					if (spuinst.JumpTarget != null)
						jumpSources[bb].AddLast(graphNode);
				}
			}

			foreach (SpuBasicBlock bb in jumpTargets.Keys)
			{
				GraphNode targetGraphNode = jumpTargets[bb];
				foreach (GraphNode sourceNode in jumpSources[bb])
				{
					flowGraph.AddEdge(sourceNode, targetGraphNode );
				}
			}

			return flowGraph;
		}

		public static InterferenceGraph CreatInterferenceGraph(FlowGraph flowGraph)
		{
			Dictionary<GraphNode, Set<VirtualRegister>> liveInDic = new Dictionary<GraphNode, Set<VirtualRegister>>();
			Dictionary<GraphNode, Set<VirtualRegister>> liveOutDic = new Dictionary<GraphNode, Set<VirtualRegister>>();

			bool calculating;

			do
			{
				calculating = false;

				foreach (GraphNode node in flowGraph.Nodes)
				{
					Set<VirtualRegister> oldLiveIn;
					Set<VirtualRegister> oldLiveOut;

					Set<VirtualRegister> liveIn = new Set<VirtualRegister>();
					Set<VirtualRegister> liveOut = new Set<VirtualRegister>();

					liveInDic.TryGetValue(node, out oldLiveIn);
					liveOutDic.TryGetValue(node, out oldLiveOut);
					
					// Beregner livein
					liveIn.AddAll(oldLiveOut);
					liveIn.Remove(flowGraph.Def(node));
					foreach (VirtualRegister vr in flowGraph.Use(node))
						liveIn.Add(vr);
					liveInDic[node] = liveIn;

					//Beregner liveout
					foreach (GraphNode succ in node.Succ)
						liveOut.AddAll(liveInDic[succ]);
					liveOutDic[node] = liveOut;

					// I første gennemløb er kan oldLiveIn og oldLiveOut være null, og dremed ikke lig med
					// det tomme set, men da algoritmen skal i normale tilfælde skal køre mindst to gange,
					// så giver det ingen problemer.
					calculating = !liveIn.Equals(oldLiveIn) || !liveOut.Equals(oldLiveOut) || calculating;
				}
			} while (calculating);

			InterferenceGraph interferenceGraph = new InterferenceGraph();

			Dictionary<VirtualRegister, GraphNode> vrNodeDic = new Dictionary<VirtualRegister, GraphNode>();

			foreach (GraphNode node in flowGraph.Nodes)
			{
				if (flowGraph.Def(node) != null)
					vrNodeDic[flowGraph.Def(node)] = interferenceGraph.NewNode(flowGraph.Def(node));

				foreach (VirtualRegister vr in flowGraph.Use(node))
					vrNodeDic[vr] = interferenceGraph.NewNode(vr);
			}

			foreach (GraphNode flowNode in flowGraph.Nodes)
			{
				Set<VirtualRegister> lives = new Set<VirtualRegister>();
				lives.AddAll(liveInDic[flowNode]);
				lives.AddAll(liveOutDic[flowNode]);

				foreach (VirtualRegister vrFrom in lives)
					foreach (VirtualRegister vrTo in lives)
						if (vrFrom != vrTo)
							interferenceGraph.AddEdge(vrNodeDic[vrFrom], vrNodeDic[vrTo]);
			}

			return interferenceGraph;
		}
	}
}

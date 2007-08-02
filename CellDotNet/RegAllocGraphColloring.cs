using System.Collections.Generic;

namespace CellDotNet
{
	class RegAllocGraphColloring
	{
		// TODO list 
		// - få plads på stakken til spilled variabler
		// - mere optimal brug af lister(brug ikke LinkedList i forbindelse med contains)
		// - det er ikke alle registre der er tilgængelig for regalloc
		//


//		private FlowGraph flowGraph;

		private MethodCompiler method;

		private List<SpuBasicBlock> basicBlocks;

		private int K; // antal af tilgængelige registre.

//		private Dictionary<Node, SpuBasicBlock> nodeToBlockDic = new Dictionary<Node, SpuBasicBlock>();
//		private Dictionary<Node, SpuInstruction> nodeToInstDic = new Dictionary<Node, SpuInstruction>();

		private Dictionary<SpuInstruction, Set<SpuInstruction>> pred = new Dictionary<SpuInstruction, Set<SpuInstruction>>();
		private Dictionary<SpuInstruction, Set<SpuInstruction>> succ = new Dictionary<SpuInstruction, Set<SpuInstruction>>();

		private Dictionary<SpuInstruction, Set<VirtualRegister>> liveIn =
			new Dictionary<SpuInstruction, Set<VirtualRegister>>();

		private Dictionary<SpuInstruction, Set<VirtualRegister>> liveOut =
			new Dictionary<SpuInstruction, Set<VirtualRegister>>();

		private Set<VirtualRegister> precolored = new Set<VirtualRegister>();

		private LinkedList<VirtualRegister> initial = new LinkedList<VirtualRegister>();

		private LinkedList<VirtualRegister> simplifyWorklist = new LinkedList<VirtualRegister>();
		private LinkedList<VirtualRegister> freezeWorklist = new LinkedList<VirtualRegister>();
		private LinkedList<VirtualRegister> spillWorklist = new LinkedList<VirtualRegister>();
		private Set<VirtualRegister> spilledNodes = new Set<VirtualRegister>();
		private Set<VirtualRegister> coalescedNodes = new Set<VirtualRegister>();
		private Set<VirtualRegister> coloredNodes = new Set<VirtualRegister>();
		private Stack<VirtualRegister> selctStack = new Stack<VirtualRegister>();

		private Set<SpuInstruction> coalescedMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> constrainedMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> frozenMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> worklistMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> activeMoves = new Set<SpuInstruction>();

		private Dictionary<VirtualRegister, Set<VirtualRegister>> adjSet =
			new Dictionary<VirtualRegister, Set<VirtualRegister>>();

		private Dictionary<VirtualRegister, Set<VirtualRegister>> adjList =
			new Dictionary<VirtualRegister, Set<VirtualRegister>>();

		private Dictionary<VirtualRegister, int> degree = new Dictionary<VirtualRegister, int>();

		private Dictionary<VirtualRegister, Set<SpuInstruction>> moveList =
			new Dictionary<VirtualRegister, Set<SpuInstruction>>();

		private Dictionary<VirtualRegister, VirtualRegister> alias = new Dictionary<VirtualRegister, VirtualRegister>();
		private Dictionary<VirtualRegister, HardwareRegister> color = new Dictionary<VirtualRegister, HardwareRegister>();

		public void Alloc(MethodCompiler methodCompiler)
		{
			method = methodCompiler;

			basicBlocks = method.SpuBasicBlocks;

			bool redoAlloc;

			K = HardwareRegister.GetCellRegisters().Count; //TODO overvej en mere smart/fleksibel måde.
			// f.eks. mulighed for dynamisk at angive antallet af tilgængelige registre.
			// eller at alloc tager som argument de registre der må bruges.

			precolored.AddAll(HardwareRegister.getPrecolored());
			initial = getInitialVirtualRegisters();
			
			do
			{
				LivenessAnalysis();
				Build();
				MakeWorklist();
				do
				{
					if (simplifyWorklist.Count != 0)
						Simplify();
					else if (worklistMoves.Count != 0)
						Coalesce();
					else if (freezeWorklist.Count != 0)
						Freeze();
					else if (spillWorklist.Count != 0)
						SelectSpill();
				} while (simplifyWorklist.Count != 0 || worklistMoves.Count != 0 || freezeWorklist.Count != 0 ||
				         spillWorklist.Count != 0);
				AssignColors();
				redoAlloc = spilledNodes.Count != 0;
				if (redoAlloc)
					RewriteProgram();
			} while (redoAlloc);
		}

		private LinkedList<VirtualRegister> getInitialVirtualRegisters()
		{
			Set<VirtualRegister> result = new Set<VirtualRegister>();

			foreach (SpuBasicBlock block in basicBlocks)
			{
				SpuInstruction inst = block.Head;

				while(inst != null)
				{
					result.Add(inst.Ra);
					result.Add(inst.Rb);
					result.Add(inst.Rc);
					result.Add(inst.Rt);

					inst = inst.Next;
				}
			}

			LinkedList<VirtualRegister> listResult = new LinkedList<VirtualRegister>();

			foreach (VirtualRegister register in result)
			{
				listResult.AddLast(register);
			}

			return listResult;
		}

/*

		public static FlowGraph CreatFlowGraph(List<SpuBasicBlock> basicBlocks)
		{
			Dictionary<SpuBasicBlock, GraphNode> jumpTargets = new Dictionary<SpuBasicBlock, GraphNode>();
			Dictionary<SpuBasicBlock, LinkedList<GraphNode>> jumpSources = new Dictionary<SpuBasicBlock, LinkedList<GraphNode>>();

			FlowGraph flowGraph = new FlowGraph();

			foreach (SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction spuinst = bb.Head;
				GraphNode graphNode = flowGraph.NewNode(spuinst.Def, spuinst.Use, false); //TODO isMove skal sættes!
				jumpTargets[bb] = graphNode;

				if (spuinst.JumpTarget != null)
					jumpSources[bb].AddLast(graphNode);

				while (spuinst.Next != null)
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
					flowGraph.AddEdge(sourceNode, targetGraphNode);
				}
			}

			return flowGraph;
		}

*/

/*
		public InterferenceGraph CreatInterferenceGraph()
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
					liveIn.Remove(flowGraph.def(node));
					foreach (VirtualRegister vr in flowGraph.use(node))
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

			//Build interferenceGraph

			InterferenceGraph interferenceGraph = new InterferenceGraph();

			Dictionary<VirtualRegister, GraphNode> vrNodeDic = new Dictionary<VirtualRegister, GraphNode>();

			foreach (GraphNode node in flowGraph.Nodes)
			{
				if (flowGraph.def(node) != null)
					vrNodeDic[flowGraph.def(node)] = interferenceGraph.NewNode(flowGraph.def(node));

				foreach (VirtualRegister vr in flowGraph.use(node))
					vrNodeDic[vr] = interferenceGraph.NewNode(vr);
			}

			foreach (GraphNode flowNode in flowGraph.Nodes)
			{
				Set<VirtualRegister> lives = new Set<VirtualRegister>();
				lives.AddAll(liveOutDic[flowNode]);

				if (flowGraph.IsMove(flowNode))
				{
					lives.RemoveAll(flowGraph.use(flowNode));
					foreach (VirtualRegister r in flowGraph.use(flowNode))
						moveList[r] = Set<GraphNode>.Add(flowNode, moveList[r]);
					worklistMoves.Add(flowNode);
				}

				lives.Add(flowGraph.def(flowNode));

				VirtualRegister def = flowGraph.def(flowNode);

				foreach (VirtualRegister l in lives)
					if (def != l)
					{
						interferenceGraph.AddEdge(vrNodeDic[def], vrNodeDic[l]);
						interferenceGraph.AddEdge(vrNodeDic[l], vrNodeDic[def]);
						//TODO AddEdge?
					}
			}

			return interferenceGraph;
		}
*/

		private void LivenessAnalysis()
		{
			Dictionary<SpuBasicBlock, SpuInstruction> jumpTargets = new Dictionary<SpuBasicBlock, SpuInstruction>();
			Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>> jumpSources =
				new Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>>();

			SpuInstruction predecessor = null;

			// Building predecessore og successor
			foreach (SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				jumpTargets[bb] = inst;

				while (inst != null)
				{
					if (predecessor != null)
					{
						succ[predecessor].Add(inst);
						pred[inst].Add(predecessor);
					}
					if (inst.JumpTarget != null)
						jumpSources[inst.JumpTarget].AddLast(inst);
					predecessor = inst;
					inst = inst.Next;
				}
			}

			// Patch predecessore og successor with jump info
			foreach (SpuBasicBlock bb in jumpTargets.Keys)
			{
				SpuInstruction i = jumpTargets[bb];
				foreach (SpuInstruction s in jumpSources[bb])
				{
					succ[s].Add(i);
					pred[i].Add(s);
				}
			}

			// Calculate live info.
			// Initialize liveIn and liveOut.
			foreach (SpuBasicBlock bb in basicBlocks)
				for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
				{
					liveIn[inst] = new Set<VirtualRegister>();
					liveOut[inst] = new Set<VirtualRegister>();
				}

			// Iterates until nochanges.
			bool reIterate;
			do
			{
				reIterate = false;

				Dictionary<SpuInstruction, Set<VirtualRegister>> oldLiveOut;
				Dictionary<SpuInstruction, Set<VirtualRegister>> oldLiveIn;
				oldLiveOut = liveOut;
				oldLiveIn = liveIn;
				foreach (SpuBasicBlock bb in basicBlocks)
					for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
					{
						liveIn[inst] = new Set<VirtualRegister>();
						liveIn[inst].AddAll(liveOut[inst]);
						liveIn[inst].Remove(inst.Def);
						liveIn[inst].AddAll(inst.Use);
						liveOut[inst] = new Set<VirtualRegister>();
						foreach (SpuInstruction s in succ[inst])
							liveOut[inst].AddAll(liveIn[s]);

						if (!reIterate)
							reIterate |= !oldLiveIn[inst].Equals(liveIn[inst]) || !oldLiveOut[inst].Equals(liveOut[inst]);
					}
			} while (reIterate);
		}

		private void Build()
		{
			foreach (SpuBasicBlock bb in basicBlocks)
			{
				for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
				{
					Set<VirtualRegister> live = new Set<VirtualRegister>();
					live.AddAll(liveOut[inst]);
					if (inst.OpCode != null && inst.OpCode == SpuOpCode.move)
					{
						live.RemoveAll(inst.Use);
						moveList[inst.Def].Add(inst);
						moveList[inst.Use[0]].Add(inst); // Move instruction only have one use register.
						worklistMoves.Add(inst);
					}
					live.Add(inst.Def);
					foreach (VirtualRegister l in live)
						AddEdge(l, inst.Def);
				}
			}
		}

		private void AddEdge(VirtualRegister from, VirtualRegister to)
		{
			if (adjSet[from].Contains(to) && from != to)
			{
				adjSet[from].Add(to);
				adjSet[to].Add(from);

				if (!precolored.Contains(from))
				{
					adjList[from].Add(to);
					degree[from]++;
				}
				if (!precolored.Contains(to))
				{
					adjList[to].Add(from);
					degree[to]++;
				}
			}
		}

		private void MakeWorklist()
		{
			foreach (VirtualRegister r in initial)
			{
				if (degree[r] >= K)
					spillWorklist.AddLast(r);
				else if (MoveRelated(r))
					freezeWorklist.AddLast(r);
				else
					simplifyWorklist.AddLast(r);
			}
		}

		private Set<VirtualRegister> Adjacent(VirtualRegister r)
		{
			Set<VirtualRegister> result = new Set<VirtualRegister>();
			result.AddAll(adjList[r]);
			result.RemoveAll(selctStack);
			result.RemoveAll(coalescedNodes);
			return result;
		}

		private Set<SpuInstruction> NodeMoves(VirtualRegister r)
		{
			Set<SpuInstruction> result = new Set<SpuInstruction>();
			foreach (SpuInstruction i in moveList[r])
			{
				if (activeMoves.Contains(i) || worklistMoves.Contains(i))
				{
					result.Add(i);
				}
			}
			return result;
		}

		private bool MoveRelated(VirtualRegister r)
		{
			return NodeMoves(r).Count != 0;
		}

		private void Simplify()
		{
			VirtualRegister r = simplifyWorklist.First.Value;
			simplifyWorklist.RemoveFirst();
			selctStack.Push(r);
			foreach (VirtualRegister ar in Adjacent(r))
			{
				DecrementDegree(ar);
			}
		}

		private void DecrementDegree(VirtualRegister r)
		{
			int d = degree[r];
			degree[r] = d - 1;
			if (d == K)
			{
				Set<VirtualRegister> a = Adjacent(r);
				a.Add(r);
				EnableMoves(a);
				spillWorklist.Remove(r); //TODO ikke optimalt på LinkedList
				if (MoveRelated(r))
					freezeWorklist.AddLast(r);
				else
					simplifyWorklist.AddLast(r);
			}
		}

		private void EnableMoves(IEnumerable<VirtualRegister> rlist)
		{
			foreach (VirtualRegister r in rlist)
				EnableMove(r);
		}

		private void EnableMove(VirtualRegister r)
		{
			foreach (SpuInstruction i in NodeMoves(r))
				if (activeMoves.Contains(i))
				{
					activeMoves.Remove(i);
					worklistMoves.Add(i);
				}
		}

		private void AddWorkList(VirtualRegister r)
		{
			if (!precolored.Contains(r) && !MoveRelated(r) && degree[r] < K)
			{
				freezeWorklist.Remove(r); //TODO Not optimal for LinkedList.
				simplifyWorklist.AddLast(r);
			}
		}

		private bool OK(VirtualRegister t, VirtualRegister r)
		{
			return degree[t] < K || precolored.Contains(t) || adjSet[t].Contains(r);
		}

		private bool Conservative(IEnumerable<VirtualRegister> rlist)
		{
			int k = 0;
			foreach (VirtualRegister r in rlist)
				if (degree[r] >= K) k++;
			return k < K;
		}

		private void Coalesce()
		{
			SpuInstruction move = worklistMoves.getItem();
			VirtualRegister x = GetAlias(move.Use[0]);
			VirtualRegister y = GetAlias(move.Def);

			VirtualRegister u;
			VirtualRegister v;

			if (precolored.Contains(y))
			{
				u = y;
				v = x;
			}
			else
			{
				u = x;
				v = y;
			}
			worklistMoves.Remove(move);

			if (u == v)
			{
				coalescedMoves.Add(move);
				AddWorkList(u);
			}
			else if (precolored.Contains(v) || adjSet[u].Contains(v))
			{
				constrainedMoves.Add(move);
				AddWorkList(u);
				AddWorkList(v);
			}
			else if (precolored.Contains(u) && OKHelper(u, v) ||
			         !precolored.Contains(u) && ConservativeHelper(Adjacent(u), Adjacent(v)))
			{
				coalescedMoves.Add(move);
				Combine(u, v);
				AddWorkList(u);
			}
			else activeMoves.Add(move);
		}

		private bool ConservativeHelper(IEnumerable<VirtualRegister> adjU, IEnumerable<VirtualRegister> adjV)
		{
			Set<VirtualRegister> set = new Set<VirtualRegister>();
			set.AddAll(adjU);
			set.AddAll(adjV);
			return Conservative(set);
		}

		private bool OKHelper(VirtualRegister u, VirtualRegister v)
		{
			foreach (VirtualRegister r in Adjacent(v))
				if (OK(r, u)) return true;

			return false;
		}

		private void Combine(VirtualRegister u, VirtualRegister v)
		{
			if (freezeWorklist.Contains(v)) //TODO LinkedList not optimal.
				freezeWorklist.Remove(v); //TODO LinkedList not optimal.
			else
				spillWorklist.Remove(v);

			coalescedNodes.Add(v);
			alias[v] = u;
			moveList[u].AddAll(moveList[v]);
			EnableMove(v);
			foreach (VirtualRegister t in Adjacent(v))
			{
				AddEdge(t, u);
				DecrementDegree(t);
			}
			if (degree[u] >= K && freezeWorklist.Contains(u))
			{
				freezeWorklist.Remove(u); //TODO LinkedList is not optimal.
				spillWorklist.AddLast(u);
			}
		}

		private VirtualRegister GetAlias(VirtualRegister r)
		{
			if (coloredNodes.Contains(r))
				return GetAlias(alias[r]);
			else
				return r;
		}

		private void Freeze()
		{
			VirtualRegister u = freezeWorklist.First.Value;
			freezeWorklist.RemoveFirst();
			simplifyWorklist.AddLast(u);
			FreezeMoves(u);
		}

		private void FreezeMoves(VirtualRegister u)
		{
			foreach (SpuInstruction move in NodeMoves(u))
			{
				VirtualRegister x = move.Def;
				VirtualRegister y = move.Use[0];

				VirtualRegister v;
				if (GetAlias(y) == GetAlias(u))
					v = GetAlias(x);
				else
					v = GetAlias(y);

				activeMoves.Remove(move);
				frozenMoves.Add(move);

				if (freezeWorklist.Contains(v) && NodeMoves(v).Count == 0)
				{
					freezeWorklist.Remove(v); // TODO LinkedList not optimal.
					simplifyWorklist.AddLast(v);
				}
			}
		}

		private void SelectSpill()
		{
			// TODO Bedre måde at vælge m på, se p. 239.
			//      Den nnuværende måde at vælge m på, lægger op til ballade.
			VirtualRegister m = spillWorklist.First.Value;
			spillWorklist.RemoveFirst();
			simplifyWorklist.AddLast(m);
			FreezeMoves(m);
		}

		private void AssignColors()
		{
			while (selctStack.Count > 0)
			{
				VirtualRegister n = selctStack.Pop();

				Set<HardwareRegister> okColors = new Set<HardwareRegister>();
				okColors.AddAll(HardwareRegister.GetCellRegisters()); //TODO overvej en mere smart/fleksibel måde.

				foreach (VirtualRegister w in adjList[n])
				{
					VirtualRegister a = GetAlias(w);

					if (coloredNodes.Contains(a) || precolored.Contains(a))
					{
						okColors.Remove(color[a]);
					}
					if (okColors.Count <= 0)
					{
						spilledNodes.Add(n);
					}
					else
					{
						coloredNodes.Add(n);
						HardwareRegister c = okColors.getItem();
						color[n] = c;
					}
				}
			}

			foreach (VirtualRegister n in coalescedNodes)
			{
				color[n] = color[GetAlias(n)];
			}
		}

		private void RewriteProgram()
		{
			Set<VirtualRegister> newTemps = new Set<VirtualRegister>();
			foreach (VirtualRegister v in spilledNodes)
			{
				foreach (SpuBasicBlock basicBlock in basicBlocks)
				{
					SpuInstruction prevInst = null;
					SpuInstruction inst = basicBlock.Head;
					while (inst != null)
					{
						if (inst.Def == v)
						{
							VirtualRegister vt = new VirtualRegister();
							newTemps.Add(vt);

							inst.Rt = vt;

							SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);

							stor.Rt = vt;
							stor.Constant = 0; // TODO Offset in frame
							stor.Ra = null; // TODO Frame pointer

							SpuInstruction next = inst.Next;
							inst.Next = stor;
							stor.Prev = inst;
							stor.Next = next;
							next.Prev = stor;

							prevInst = stor;
							inst = prevInst.Next;
						}
						else if (inst.Ra == v || inst.Rb == v || inst.Rc == v || inst.Rt == v)
						{
							VirtualRegister vt = new VirtualRegister();
							newTemps.Add(vt);

							// Det antages at et register kun kan bruges en gang i en instruktion.
							if (inst.Ra == v)
								inst.Ra = vt;
							else if (inst.Rb == v)
								inst.Rb = vt;
							else if (inst.Rc == v)
								inst.Rc = vt;
							else if (inst.Rt == v)
								inst.Rt = vt;

							SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

							load.Rt = vt;
							load.Constant = 0; // TODO Offset in frame
							load.Ra = null; // TODO Frame pointer

							if (prevInst != null)
							{
								prevInst.Next = load;
								load.Prev = prevInst;
							}
							else
							{
								basicBlock.Head = load;
							}

							load.Next = inst;
							inst.Prev = load;

							prevInst = inst;
							inst = prevInst.Next;
						}
						else
						{
							prevInst = inst;
							inst = prevInst.Next;
						}
					}
				}
			}
		}
	}
}
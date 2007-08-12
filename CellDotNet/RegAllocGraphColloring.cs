using System.Collections.Generic;

namespace CellDotNet
{
	class RegAllocGraphColloring
	{
		// TODO list 
		// - OK få plads på stakken til spilled variabler
		// - mere optimal brug af lister(brug ikke LinkedList i forbindelse med contains)
		// - OK det er ikke alle registre der er tilgængelig for regalloc
		//

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

		private BitMatrix adjMatrix;

		private Dictionary<VirtualRegister, Set<VirtualRegister>> adjList =
			new Dictionary<VirtualRegister, Set<VirtualRegister>>();

		private Dictionary<VirtualRegister, int> degree = new Dictionary<VirtualRegister, int>();

		private Dictionary<VirtualRegister, Set<SpuInstruction>> moveList =
			new Dictionary<VirtualRegister, Set<SpuInstruction>>();

		private Dictionary<VirtualRegister, VirtualRegister> alias = new Dictionary<VirtualRegister, VirtualRegister>();
		private Dictionary<VirtualRegister, CellRegister> color = new Dictionary<VirtualRegister, CellRegister>();

		private Dictionary<VirtualRegister, int> virtualRegisteWeight = new Dictionary<VirtualRegister, int>();

		// TODO evt. tage kode og en delagate, der kan allokere plads i frames, som argument.
		public void Alloc(MethodCompiler methodCompiler)
		{
			method = methodCompiler;

			basicBlocks = method.SpuBasicBlocks;

//			basicBlocks = new List<SpuBasicBlock>();
//
//			for(int i = 1; i < method.SpuBasicBlocks.Count-1; i++)
//				basicBlocks.Add(method.SpuBasicBlocks[i]);

			bool redoAlloc;

			K = 0;
			K += HardwareRegister.getCallerSavesCellRegisters().Length;
			K += HardwareRegister.getCalleeSavesCellRegisters().Length;
			//TODO overvej en mere smart/fleksibel måde.
			// f.eks. mulighed for dynamisk at angive antallet af tilgængelige registre.
			// eller at alloc tager som argument de registre der må bruges.

			precolored.AddAll(HardwareRegister.VirtualHardwareRegisters);
			initial = getInitialVirtualRegisters();
			
			do
			{
				pred.Clear();
				succ.Clear();

				liveIn.Clear();
				liveOut.Clear();

				degree.Clear();

				adjSet.Clear();
				adjList.Clear();

				moveList.Clear();

				foreach (SpuBasicBlock block in basicBlocks)
				{
					SpuInstruction inst = block.Head;

					while (inst != null)
					{
						pred[inst] = new Set<SpuInstruction>();
						succ[inst] = new Set<SpuInstruction>();

						liveIn[inst] = new Set<VirtualRegister>();
						liveOut[inst] = new Set<VirtualRegister>();

						if(inst.Ra != null)
						{
							adjSet[inst.Ra] = new Set<VirtualRegister>();
							adjList[inst.Ra] = new Set<VirtualRegister>();
							moveList[inst.Ra]  = new Set<SpuInstruction>();
							degree[inst.Ra] = 0;
						}
						if (inst.Rb != null)
						{
							adjSet[inst.Rb] = new Set<VirtualRegister>();
							adjList[inst.Rb] = new Set<VirtualRegister>();
							moveList[inst.Rb] = new Set<SpuInstruction>();
							degree[inst.Rb] = 0;
						}
						if (inst.Rc != null)
						{
							adjSet[inst.Rc] = new Set<VirtualRegister>();
							adjList[inst.Rc] = new Set<VirtualRegister>();
							moveList[inst.Rc] = new Set<SpuInstruction>();
							degree[inst.Rc] = 0;
						}
						if (inst.Rt != null)
						{
							adjSet[inst.Rt] = new Set<VirtualRegister>();
							adjList[inst.Rt] = new Set<VirtualRegister>();
							moveList[inst.Rt] = new Set<SpuInstruction>();
							degree[inst.Rt] = 0;
						}
						inst = inst.Next;
					}
				}


				simplifyWorklist.Clear();
				freezeWorklist.Clear();
				spillWorklist.Clear();
				spilledNodes.Clear();
				coalescedNodes.Clear();
				coloredNodes.Clear();
				selctStack.Clear();

				coalescedMoves.Clear();
				constrainedMoves.Clear();
				frozenMoves.Clear();
				worklistMoves.Clear();
				activeMoves.Clear();

				alias.Clear();

				color.Clear();
				
				foreach (VirtualRegister register in HardwareRegister.VirtualHardwareRegisters)
				{
					color[register] = register.Register;
				}

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

			setColors();
		}

		private void setColors()
		{
			foreach (SpuBasicBlock block in basicBlocks)
			{
				SpuInstruction inst = block.Head;

				while (inst != null)
				{
					if (inst.Ra != null)
						inst.Ra.Register = color[inst.Ra];
					if (inst.Rb != null)
						inst.Rb.Register = color[inst.Rb];
					if (inst.Rc != null)
						inst.Rc.Register = color[inst.Rc];
					if (inst.Rt != null)
						inst.Rt.Register = color[inst.Rt];

					inst = inst.Next;
				}
			}
		}

		private LinkedList<VirtualRegister> getInitialVirtualRegisters()
		{
			Set<VirtualRegister> result = new Set<VirtualRegister>();

			foreach (SpuBasicBlock block in basicBlocks)
			{
				SpuInstruction inst = block.Head;

				while(inst != null)
				{
					if (inst.Ra != null && !inst.Ra.IsRegisterSet)
						result.Add(inst.Ra);
					if (inst.Rb != null && !inst.Rb.IsRegisterSet)
						result.Add(inst.Rb);
					if (inst.Rc != null && !inst.Rc.IsRegisterSet)
						result.Add(inst.Rc);
					if (inst.Rt != null && !inst.Rt.IsRegisterSet)
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

		private void LivenessAnalysis()
		{
			Dictionary<SpuBasicBlock, SpuInstruction> jumpTargets = new Dictionary<SpuBasicBlock, SpuInstruction>();
			Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>> jumpSources =
				new Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>>();

			SpuInstruction predecessor = null;

			foreach (SpuBasicBlock bb in basicBlocks)
				jumpSources[bb] = new LinkedList<SpuInstruction>();

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

//				Dictionary<SpuInstruction, Set<VirtualRegister>> oldLiveOut = new Dictionary<SpuInstruction, Set<VirtualRegister>>();
//				Dictionary<SpuInstruction, Set<VirtualRegister>> oldLiveIn = new Dictionary<SpuInstruction, Set<VirtualRegister>>();
//
//				foreach (SpuInstruction i in liveOut.Keys)
//				{
//					oldLiveOut[i] = new Set<VirtualRegister>();
//					oldLiveOut[i].AddAll(liveOut[i]);
//				}
//
//
//				oldLiveOut = liveOut;
//				oldLiveIn = liveIn;
				for(int i = basicBlocks.Count-1; i >= 0; i--)
				{
					SpuBasicBlock bb = basicBlocks[i];
					SpuInstruction lastinst = null;
					for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
						lastinst = inst;

					for (SpuInstruction inst = lastinst; inst != null; inst = inst.Prev)
					{
						Set<VirtualRegister> oldLiveIn = new Set<VirtualRegister>();
						oldLiveIn.AddAll(liveIn[inst]);

						Set<VirtualRegister> oldLiveOut = new Set<VirtualRegister>();
						oldLiveOut.AddAll(liveOut[inst]);

						liveOut[inst] = new Set<VirtualRegister>();
						foreach (SpuInstruction s in succ[inst])
							liveOut[inst].AddAll(liveIn[s]);
						
						liveIn[inst] = new Set<VirtualRegister>();
						liveIn[inst].AddAll(liveOut[inst]);
						if (inst.Def != null)
							liveIn[inst].Remove(inst.Def);
						liveIn[inst].AddAll(inst.Use);

						if (!reIterate)
							reIterate |= !oldLiveIn.Equals(liveIn[inst]) || !oldLiveOut.Equals(liveOut[inst]);
					}
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
					if (inst.Def != null)
					{
						live.Add(inst.Def);
						foreach (VirtualRegister l in live)
							AddEdge(l, inst.Def);
					}
				}
			}
		}

		private void AddEdge(VirtualRegister from, VirtualRegister to)
		{
			if (!adjSet[from].Contains(to) && from != to)
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
			if (coalescedNodes.Contains(r))
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

			VirtualRegister m = null;

			foreach (VirtualRegister register in spillWorklist)
			{
				int weight;

				if (!virtualRegisteWeight.TryGetValue(register, out weight) || weight < 100)
				{
					m = register;
					spillWorklist.Remove(register);
					break;
				}

			}

//			VirtualRegister m = spillWorklist.First.Value;
//			spillWorklist.RemoveFirst();
			simplifyWorklist.AddLast(m);
			FreezeMoves(m);
		}

		private void AssignColors()
		{
			while (selctStack.Count > 0)
			{
				VirtualRegister n = selctStack.Pop();

				Set<CellRegister> okColors = new Set<CellRegister>();
				okColors.AddAll(HardwareRegister.getCallerSavesCellRegisters());
				okColors.AddAll(HardwareRegister.getCalleeSavesCellRegisters());

				foreach (VirtualRegister w in adjList[n])
				{
					VirtualRegister a = GetAlias(w);

					if (coloredNodes.Contains(a) || precolored.Contains(a))
						okColors.Remove(color[a]);
				}
				if (okColors.Count <= 0)
				{
					spilledNodes.Add(n);
				}
				else
				{
					coloredNodes.Add(n);
					CellRegister c = okColors.getItem();
					color[n] = c;
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
							virtualRegisteWeight[vt] = int.MaxValue;
							newTemps.Add(vt);

							inst.Rt = vt;

							SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);

							stor.Rt = vt;
							stor.Constant = method.GetNewSpillOffset();
							stor.Ra = HardwareRegister.SP;

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
							virtualRegisteWeight[vt] = int.MaxValue;
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
							load.Constant = method.GetNewSpillOffset();
							load.Ra = HardwareRegister.SP;

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
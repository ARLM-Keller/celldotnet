using System;
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

		private List<SpuBasicBlock> basicBlocks;

		private NewSpillOffsetDelegate NewSpillOffset;

		private int K; // antal af tilgængelige registre.

//		private Dictionary<Node, SpuBasicBlock> nodeToBlockDic = new Dictionary<Node, SpuBasicBlock>();
//		private Dictionary<Node, SpuInstruction> nodeToInstDic = new Dictionary<Node, SpuInstruction>();

		private Dictionary<SpuInstruction, Set<SpuInstruction>> pred = new Dictionary<SpuInstruction, Set<SpuInstruction>>();
		private Dictionary<SpuInstruction, Set<SpuInstruction>> succ = new Dictionary<SpuInstruction, Set<SpuInstruction>>();

		private Dictionary<SpuInstruction, BitVector> liveIn =
			new Dictionary<SpuInstruction, BitVector>();

		private Dictionary<SpuInstruction, BitVector> liveOut =
			new Dictionary<SpuInstruction, BitVector>();

		private BitVector precolored = new BitVector();

		private LinkedList<uint> initial = new LinkedList<uint>();

		private LinkedList<uint> simplifyWorklist = new LinkedList<uint>();
		private LinkedList<uint> freezeWorklist = new LinkedList<uint>();
		private LinkedList<uint> spillWorklist = new LinkedList<uint>();
		private BitVector spilledNodes = new BitVector();
		private BitVector coalescedNodes = new BitVector();
		private BitVector coloredNodes = new BitVector();
		private Stack<uint> selectStack = new Stack<uint>();
		private BitVector selectStackBitVector = new BitVector();

		private Set<SpuInstruction> coalescedMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> constrainedMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> frozenMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> worklistMoves = new Set<SpuInstruction>();
		private Set<SpuInstruction> activeMoves = new Set<SpuInstruction>();

//		private Dictionary<uint, Set<uint>> adjSet =
//			new Dictionary<uint, Set<uint>>();

		private BitMatrix adjMatrix = new BitMatrix(10,10);

//		private Dictionary<uint, BitVector> adjList =
//			new Dictionary<uint, BitVector>();

		private Dictionary<uint, BitVector> adjList =
			new Dictionary<uint, BitVector>();


//		private Dictionary<uint, int> degree = new Dictionary<uint, int>();
		private int[] degree;

		private Dictionary<uint, Set<SpuInstruction>> moveList =
			new Dictionary<uint, Set<SpuInstruction>>();

		private Dictionary<uint, uint> alias = new Dictionary<uint, uint>();
		private Dictionary<uint, CellRegister> color = new Dictionary<uint, CellRegister>();

		private Dictionary<uint, int> virtualRegisteWeight = new Dictionary<uint, int>();


		//TODO hukinitialisering af intToReg og regToInt.
		private List<VirtualRegister> intToReg = new List<VirtualRegister>();
		private Dictionary<VirtualRegister, uint> regToInt = new Dictionary<VirtualRegister, uint>();

		public delegate int NewSpillOffsetDelegate();

		// TODO evt. tage kode og en delagate, der kan allokere plads i frames, som argument.
		public void Alloc(List<SpuBasicBlock> inputBasicBlocks, NewSpillOffsetDelegate inputNewSpillOffset)
		{
			basicBlocks = inputBasicBlocks;

			NewSpillOffset = inputNewSpillOffset;

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

//			precolored.AddAll(HardwareRegister.VirtualHardwareRegisters);

			foreach (VirtualRegister register in HardwareRegister.VirtualHardwareRegisters)
			{
				precolored.Add(intToReg.Count);
				regToInt[register] = (uint) intToReg.Count;
				intToReg.Add(register);
			}

			initial = getInitialVirtualRegisters();
			
			do
			{
				pred.Clear();
				succ.Clear();

				liveIn.Clear();
				liveOut.Clear();

//				degree.Clear();

//				adjSet.Clear();
				adjMatrix.clear();

				adjList.Clear();

				moveList.Clear();

				foreach (SpuBasicBlock block in basicBlocks)
				{
					SpuInstruction inst = block.Head;

					while (inst != null)
					{
						pred[inst] = new Set<SpuInstruction>();
						succ[inst] = new Set<SpuInstruction>();

						liveIn[inst] = new BitVector();
						liveOut[inst] = new BitVector();

//						if(inst.Ra != null)
//						{
//							adjSet[regToInt[inst.Ra]] = new Set<uint>();
//							adjList[regToInt[inst.Ra]] = new Set<uint>();
//							moveList[regToInt[inst.Ra]]  = new Set<SpuInstruction>();
//							degree[regToInt[inst.Ra]] = 0;
//						}
//						if (inst.Rb != null)
//						{
//							adjSet[regToInt[inst.Rb]] = new Set<uint>();
//							adjList[regToInt[inst.Rb]] = new Set<uint>();
//							moveList[regToInt[inst.Rb]] = new Set<SpuInstruction>();
//							degree[regToInt[inst.Rb]] = 0;
//						}
//						if (inst.Rc != null)
//						{
//							adjSet[regToInt[inst.Rc]] = new Set<uint>();
//							adjList[regToInt[inst.Rc]] = new Set<uint>();
//							moveList[regToInt[inst.Rc]] = new Set<SpuInstruction>();
//							degree[regToInt[inst.Rc]] = 0;
//						}
//						if (inst.Rt != null)
//						{
//							adjSet[regToInt[inst.Rt]] = new Set<uint>();
//							adjList[regToInt[inst.Rt]] = new Set<uint>();
//							moveList[regToInt[inst.Rt]] = new Set<SpuInstruction>();
//							degree[regToInt[inst.Rt]] = 0;
//						}

						inst = inst.Next;
					}
				}

				int maxRegNum = 0;

				foreach (uint r in initial)
				{
//					adjSet[r] = new Set<uint>();
					adjList[r] = new BitVector();
					moveList[r] = new Set<SpuInstruction>();
//					degree[r] = 0;
					maxRegNum = (int)r > maxRegNum ? (int) r : maxRegNum;
				}

				foreach (int r in precolored)
				{
//					adjSet[r] = new Set<uint>();
					adjList[(uint) r] = new BitVector();
					moveList[(uint) r] = new Set<SpuInstruction>();
//					degree[(uint) r] = 0;
					maxRegNum = r > maxRegNum ? r : maxRegNum;
				}

				degree = new int[maxRegNum+1];

				simplifyWorklist.Clear();
				freezeWorklist.Clear();
				spillWorklist.Clear();
				spilledNodes.Clear();
				coalescedNodes.Clear();
				coloredNodes.Clear();
				selectStack.Clear();
				selectStackBitVector.Clear();

				coalescedMoves.Clear();
				constrainedMoves.Clear();
				frozenMoves.Clear();
				worklistMoves.Clear();
				activeMoves.Clear();

				alias.Clear();

				color.Clear();
				
				foreach (VirtualRegister register in HardwareRegister.VirtualHardwareRegisters)
				{
					color[regToInt[register]] = register.Register;
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
//				redoAlloc = spilledNodes.Count != 0;
				redoAlloc = !spilledNodes.IsCountZero();
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
//					if (inst.Ra != null)
//						inst.Ra.Register = color[regToInt[inst.Ra]];
//					if (inst.Rb != null)
//						inst.Rb.Register = color[regToInt[inst.Rb]];
//					if (inst.Rc != null)
//						inst.Rc.Register = color[regToInt[inst.Rc]];
//					if (inst.Rt != null)
//						inst.Rt.Register = color[regToInt[inst.Rt]];

					if (inst.Ra != null)
						inst.Ra = intToReg[(int)color[regToInt[inst.Ra]]];
					if (inst.Rb != null)
						inst.Rb = intToReg[(int)color[regToInt[inst.Rb]]];
					if (inst.Rc != null)
						inst.Rc = intToReg[(int)color[regToInt[inst.Rc]]];
					if (inst.Rt != null)
						inst.Rt = intToReg[(int)color[regToInt[inst.Rt]]];

					inst = inst.Next;
				}
			}
		}

		public static void RemoveRedundantMoves(List<SpuBasicBlock> basicBlocks)
		{
			foreach (SpuBasicBlock block in basicBlocks)
			{
				SpuInstruction inst = block.Head;

				while (inst != null)
				{
					if(inst.OpCode == SpuOpCode.move && inst.Ra == inst.Rt)
					{
						if(inst.Prev != null)
						{
							inst.Prev.Next = inst.Next;
						}
						else
						{
							block.Head = inst.Next;
						}
						if (inst.Next != null)
							inst.Next.Prev = inst.Prev;
					}
					inst = inst.Next;
				}
			}
		}

		private LinkedList<uint> getInitialVirtualRegisters()
		{
			BitVector result = new BitVector();

			foreach (SpuBasicBlock block in basicBlocks)
			{
				SpuInstruction inst = block.Head;

				while(inst != null)
				{
					if (inst.Ra != null && !inst.Ra.IsRegisterSet)
					{
						regToInt[inst.Ra] = (uint)intToReg.Count;
						intToReg.Add(inst.Ra);

						result.Add((int) regToInt[inst.Ra]);
					}
					if (inst.Rb != null && !inst.Rb.IsRegisterSet)
					{
						regToInt[inst.Rb] = (uint)intToReg.Count;
						intToReg.Add(inst.Rb);

						result.Add((int)regToInt[inst.Rb]);
					}
					if (inst.Rc != null && !inst.Rc.IsRegisterSet)
					{
						regToInt[inst.Rc] = (uint)intToReg.Count;
						intToReg.Add(inst.Rc);

						result.Add((int)regToInt[inst.Rc]);
					}
					if (inst.Rt != null && !inst.Rt.IsRegisterSet)
					{
						regToInt[inst.Rt] = (uint)intToReg.Count;
						intToReg.Add(inst.Rt);

						result.Add((int)regToInt[inst.Rt]);
					}

					inst = inst.Next;
				}
			}

			LinkedList<uint> listResult = new LinkedList<uint>();

			foreach (int register in result)
			{
				listResult.AddLast((uint) register);
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
					liveIn[inst] = new BitVector();
					liveOut[inst] = new BitVector();
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
						BitVector oldLiveIn = new BitVector();
						oldLiveIn.AddAll(liveIn[inst]);

						BitVector oldLiveOut = new BitVector();
						oldLiveOut.AddAll(liveOut[inst]);

						liveOut[inst] = new BitVector();
						foreach (SpuInstruction s in succ[inst])
							liveOut[inst].AddAll(liveIn[s]);

						liveIn[inst] = new BitVector();
						liveIn[inst].AddAll(liveOut[inst]);
						if (inst.Def != null)
							liveIn[inst].Remove((int) regToInt[inst.Def]);
//						liveIn[inst].AddAll(inst.Use);
						foreach (VirtualRegister register in inst.Use)
							liveIn[inst].Add((int) regToInt[register]);


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
					BitVector live = new BitVector();
					live.AddAll(liveOut[inst]);
					if (inst.OpCode != null && inst.OpCode == SpuOpCode.move)
					{
//						live.RemoveAll(inst.Use);
						foreach (VirtualRegister register in inst.Use)
							live.Remove((int) regToInt[register]);

						moveList[regToInt[inst.Def]].Add(inst);
						moveList[regToInt[inst.Use[0]]].Add(inst); // Move instruction only have one use register.
						worklistMoves.Add(inst);
					}
					if (inst.Def != null)
					{
						live.Add((int) regToInt[inst.Def]);
						foreach (int l in live)
							AddEdge((uint) l, regToInt[inst.Def]);
					}
				}
			}
		}

		private void AddEdge(uint from, uint to)
		{
//			if (!adjSet[from].Contains(to) && from != to)
			if (!adjMatrix.contains((int) from, (int) to) && from != to)
				{
//				adjSet[from].Add(to);
//				adjSet[to].Add(from);

				adjMatrix.add((int) from, (int) to);
				adjMatrix.add((int) to, (int) from);

				if (!precolored.Contains((int) from))
				{
					adjList[from].Add((int) to);
					degree[from]++;
				}
				if (!precolored.Contains((int) to))
				{
					adjList[to].Add((int) from);
					degree[to]++;
				}
			}
		}

		private void MakeWorklist()
		{
			foreach (uint r in initial)
			{
				if (degree[r] >= K)
					spillWorklist.AddLast(r);
				else if (MoveRelated(r))
					freezeWorklist.AddLast(r);
				else
					simplifyWorklist.AddLast(r);
			}
		}

		private BitVector Adjacent(uint r)
		{
			BitVector result = new BitVector();
			result.AddAll(adjList[r]);
			result.RemoveAll(selectStackBitVector);
			result.RemoveAll(coalescedNodes);
			return result;
		}

		private Set<SpuInstruction> NodeMoves(uint r)
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

		private bool MoveRelated(uint r)
		{
			return NodeMoves(r).Count != 0;
		}

		private void Simplify()
		{
			uint r = simplifyWorklist.First.Value;
			simplifyWorklist.RemoveFirst();
			selectStack.Push(r);
			selectStackBitVector.Add((int) r);
			foreach (int ar in Adjacent(r))
			{
				DecrementDegree((uint) ar);
			}
		}

		private void DecrementDegree(uint r)
		{
			int d = degree[r];
			degree[r] = d - 1;
			if (d == K)
			{
				BitVector a = Adjacent(r);
				a.Add((int) r);
				EnableMoves(a);
				spillWorklist.Remove(r); //TODO ikke optimalt på LinkedList
				if (MoveRelated(r))
					freezeWorklist.AddLast(r);
				else
					simplifyWorklist.AddLast(r);
			}
		}

		private void EnableMoves(BitVector rlist)
		{
			foreach (int r in rlist)
				EnableMove((uint) r);
		}

		private void EnableMove(uint r)
		{
			foreach (SpuInstruction i in NodeMoves(r))
				if (activeMoves.Contains(i))
				{
					activeMoves.Remove(i);
					worklistMoves.Add(i);
				}
		}

		private void AddWorkList(uint r)
		{
			if (!precolored.Contains((int) r) && !MoveRelated(r) && degree[r] < K)
			{
				freezeWorklist.Remove(r); //TODO Not optimal for LinkedList.
				simplifyWorklist.AddLast(r);
			}
		}

		private bool OK(uint t, uint r)
		{
//			return degree[t] < K || precolored.Contains(t) || adjSet[t].Contains(r);
			return degree[t] < K || precolored.Contains((int) t) || adjMatrix.contains((int) t, (int) r);
		}

		private bool Conservative(BitVector rlist)
		{
			int k = 0;
			foreach (int r in rlist)
				if (degree[(uint) r] >= K) k++;
			return k < K;
		}

		private void Coalesce()
		{
			SpuInstruction move = worklistMoves.getItem();
			uint x = GetAlias(regToInt[move.Use[0]]);
			uint y = GetAlias(regToInt[move.Def]);

			uint u;
			uint v;

			if (precolored.Contains((int) y))
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
//			else if (precolored.Contains(v) || adjSet[u].Contains(v))
			else if (precolored.Contains((int) v) || adjMatrix.contains((int) u, (int) v))
			{
				constrainedMoves.Add(move);
				AddWorkList(u);
				AddWorkList(v);
			}
			else if (precolored.Contains((int) u) && OKHelper(u, v) ||
			         !precolored.Contains((int) u) && ConservativeHelper(Adjacent(u), Adjacent(v)))
			{
				coalescedMoves.Add(move);
				Combine(u, v);
				AddWorkList(u);
			}
			else activeMoves.Add(move);
		}

		private bool ConservativeHelper(BitVector adjU, BitVector adjV)
		{
			BitVector set = new BitVector();
			set.AddAll(adjU);
			set.AddAll(adjV);
			return Conservative(set);
		}

		private bool OKHelper(uint u, uint v)
		{
			foreach (int r in Adjacent(v))
				if (OK((uint) r, u)) return true;

			return false;
		}

		private void Combine(uint u, uint v)
		{
			if (freezeWorklist.Contains(v)) //TODO LinkedList not optimal.
				freezeWorklist.Remove(v); //TODO LinkedList not optimal.
			else
				spillWorklist.Remove(v);

			coalescedNodes.Add((int) v);
			alias[v] = u;
			moveList[u].AddAll(moveList[v]);
			EnableMove(v);
			foreach (int t in Adjacent(v))
			{
				AddEdge((uint) t, u);
				DecrementDegree((uint) t);
			}
			if (degree[u] >= K && freezeWorklist.Contains(u))
			{
				freezeWorklist.Remove(u); //TODO LinkedList is not optimal.
				spillWorklist.AddLast(u);
			}
		}

		private uint GetAlias(uint r)
		{
			if (coalescedNodes.Contains((int) r))
				return GetAlias(alias[r]);
			else
				return r;
		}

		private void Freeze()
		{
			uint u = freezeWorklist.First.Value;
			freezeWorklist.RemoveFirst();
			simplifyWorklist.AddLast(u);
			FreezeMoves(u);
		}

		private void FreezeMoves(uint u)
		{
			foreach (SpuInstruction move in NodeMoves(u))
			{
				uint x = regToInt[move.Def];
				uint y = regToInt[move.Use[0]];

				uint v;
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

			uint m = 0;
			bool found = false;

			foreach (uint register in spillWorklist)
			{
				int weight;

				if (!virtualRegisteWeight.TryGetValue(register, out weight) || weight < 100)
				{
					m = register;
					spillWorklist.Remove(register);
					found = true;
					break;
				}
			}

			if(!found)
			{
				throw new Exception("Not able to spill.");
			}

//			VirtualRegister m = spillWorklist.First.Value;
//			spillWorklist.RemoveFirst();
			simplifyWorklist.AddLast(m);
			FreezeMoves(m);
		}

		private void AssignColors()
		{
			while (selectStack.Count > 0)
			{
				uint n = selectStack.Pop();
				selectStackBitVector.Remove((int) n);

				Set<CellRegister> okColors = new Set<CellRegister>(HardwareRegister.getCallerSavesCellRegisters().Length + HardwareRegister.getCalleeSavesCellRegisters().Length);
				okColors.AddAll(HardwareRegister.getCallerSavesCellRegisters());
				okColors.AddAll(HardwareRegister.getCalleeSavesCellRegisters());

				foreach (int w in adjList[n])
				{
					uint a = GetAlias((uint) w);

					if (coloredNodes.Contains((int) a) || precolored.Contains((int) a))
						okColors.Remove(color[a]);
				}
				if (okColors.Count <= 0)
				{
					spilledNodes.Add((int) n);
				}
				else
				{
					coloredNodes.Add((int) n);
					CellRegister c = okColors.getItem();
					color[n] = c;
				}
				
			}

			foreach (int n in coalescedNodes)
			{
				color[(uint) n] = color[GetAlias((uint) n)];
			}
		}

		private void RewriteProgram()
		{
			Set<uint> newTemps = new Set<uint>();
			foreach (int vint in spilledNodes)
			{
				VirtualRegister v = intToReg[vint];

				foreach (SpuBasicBlock basicBlock in basicBlocks)
				{
					SpuInstruction prevInst = null;
					SpuInstruction inst = basicBlock.Head;
					while (inst != null)
					{
						if (inst.Def == v)
						{
							VirtualRegister vt = new VirtualRegister();

							regToInt[vt] = (uint)intToReg.Count;
							intToReg.Add(vt);

							virtualRegisteWeight[regToInt[vt]] = int.MaxValue;
							newTemps.Add(regToInt[vt]);

							inst.Rt = vt;

							SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);

							stor.Rt = vt;
							stor.Constant = NewSpillOffset();
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

							regToInt[vt] = (uint)intToReg.Count;
							intToReg.Add(vt);

							virtualRegisteWeight[regToInt[vt]] = int.MaxValue;
							newTemps.Add(regToInt[vt]);

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
							load.Constant = NewSpillOffset();
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
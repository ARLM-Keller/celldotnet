using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// The graph coloring register allocator.
	/// It is currently not used (20071012).
	/// </summary>
	class RegAllocGraphColloring
	{
		// TODO list 
		// - OK få plads på stakken til spilled variabler
		// - mere optimal brug af lister(brug ikke LinkedList i forbindelse med contains)
		// - OK det er ikke alle registre der er tilgængelig for regalloc
		// - Register nummer counter.

		private List<SpuBasicBlock> basicBlocks;

		private StackSpaceAllocator _StackSpaceAllocator;

		private int maxInstNr; // note første inst har nr = 1

		private int maxRegNum;

		private int K; // antal af tilgængelige registre.

//		private Dictionary<Node, SpuBasicBlock> nodeToBlockDic = new Dictionary<Node, SpuBasicBlock>();
//		private Dictionary<Node, SpuInstruction> nodeToInstDic = new Dictionary<Node, SpuInstruction>();

		private BitVector[] pred;
		private BitVector[] succ;

//		private Dictionary<SpuInstruction, Set<SpuInstruction>> pred = new Dictionary<SpuInstruction, Set<SpuInstruction>>();
//		private Dictionary<SpuInstruction, Set<SpuInstruction>> succ = new Dictionary<SpuInstruction, Set<SpuInstruction>>();

		private BitVector[] liveIn;
		private BitVector[] liveOut;

//		private Dictionary<SpuInstruction, BitVector> liveIn =
//			new Dictionary<SpuInstruction, BitVector>();
//
//		private Dictionary<SpuInstruction, BitVector> liveOut =
//			new Dictionary<SpuInstruction, BitVector>();

		private BitVector precolored = new BitVector();
		private LinkedList<uint> initial = new LinkedList<uint>();

		private LinkedList<uint> simplifyWorklist = new LinkedList<uint>();
		private LinkedList<uint> freezeWorklist = new LinkedList<uint>();
		private LinkedList<uint> spillWorklist = new LinkedList<uint>();
		private BitVector spilledNodes = new BitVector();
		private BitVector coalescedNodes = new BitVector(); // 
		private BitVector coloredNodes = new BitVector(); // ingen Vector op
		private Stack<uint> selectStack = new Stack<uint>();
		private BitVector selectStackBitVector = new BitVector();

		private BitVector coalescedMoves = new BitVector();
		private BitVector constrainedMoves = new BitVector();
		private BitVector frozenMoves = new BitVector();
		private BitVector worklistMoves = new BitVector(); // set op
		private BitVector activeMoves = new BitVector(); // set op

//		private Set<SpuInstruction> coalescedMoves = new Set<SpuInstruction>();
//		private Set<SpuInstruction> constrainedMoves = new Set<SpuInstruction>();
//		private Set<SpuInstruction> frozenMoves = new Set<SpuInstruction>();
//		private Set<SpuInstruction> worklistMoves = new Set<SpuInstruction>();
//		private Set<SpuInstruction> activeMoves = new Set<SpuInstruction>();

//		private Dictionary<uint, Set<uint>> adjSet =
//			new Dictionary<uint, Set<uint>>();

		private BitMatrix adjMatrix = new BitMatrix();

//		private Dictionary<uint, BitVector> adjList =
//			new Dictionary<uint, BitVector>();

		private BitVector[] adjList; // set op

//		private Dictionary<uint, int> degree = new Dictionary<uint, int>();
		private int[] degree;

		private BitVector[] moveList; // set op


//		private Dictionary<uint, Set<SpuInstruction>> moveList =
//			new Dictionary<uint, Set<SpuInstruction>>();

		private CellRegister[] color;
//		private Dictionary<uint, CellRegister> color = new Dictionary<uint, CellRegister>();

//		private Dictionary<uint, uint> alias = new Dictionary<uint, uint>();
		private uint[] alias;

		// Only used for invariance test. Not nessesary for the algorithem to work.
		private BitVector selectedForSpill;

		// maps from virtual reg to weight. Is not cleared for each iteration.
		private Dictionary<VirtualRegister, int> virtualRegisteWeight = new Dictionary<VirtualRegister, int>();
//		private List<int> virtualRegisteWeight;
//		private int[] virtualRegisteWeight;

		private List<VirtualRegister> intToReg = new List<VirtualRegister>();
		private Dictionary<VirtualRegister, uint> regToInt = new Dictionary<VirtualRegister, uint>();

		private List<SpuInstruction> intToSpuInst = new List<SpuInstruction>();
		private Dictionary<SpuInstruction, int> spuinstToInt = new Dictionary<SpuInstruction, int>();

		private BitVector callerSavesRegister = new BitVector();

		private static int allocCalls;
		private int allocCount;
		private int allocLoopCount;

		// TODO evt. tage kode og en delagate, der kan allokere plads i frames, som argument.
		public void Alloc(List<SpuBasicBlock> inputBasicBlocks, StackSpaceAllocator inputStackSpaceAllocator, Dictionary<VirtualRegister, int> inputRegisterWeight)
		{
			allocCalls++;

			basicBlocks = inputBasicBlocks;

			_StackSpaceAllocator = inputStackSpaceAllocator;

//			basicBlocks = new List<SpuBasicBlock>();
//
//			for(int i = 1; i < method.SpuBasicBlocks.Count-1; i++)
//				basicBlocks.Add(method.SpuBasicBlocks[i]);

			bool redoAlloc;

			K = 0;
			K += HardwareRegister.GetCallerSavesCellRegisters().Length;
			K += HardwareRegister.GetCalleeSavesCellRegisters().Length;
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

			foreach (uint r in initial)
			{
				maxRegNum = (int) r > maxRegNum ? (int) r : maxRegNum;
				virtualRegisteWeight.Add(intToReg[(int) r], 50);
			}

			foreach (int r in precolored)
			{
				maxRegNum = r > maxRegNum ? r : maxRegNum;
				virtualRegisteWeight.Add(intToReg[r], int.MaxValue);
			}

			if (inputRegisterWeight != null)
				foreach (VirtualRegister r in inputRegisterWeight.Keys)
				{
					virtualRegisteWeight[r] = inputRegisterWeight[r];
				}

			do
			{
				// NOTE: for at list test skal virke, skal denne del foratages hvergang.

				precolored.Clear();
				regToInt.Clear();
				intToReg.Clear();
				maxRegNum = 0;
//				virtualRegisteWeight.Clear();

				foreach (VirtualRegister register in HardwareRegister.VirtualHardwareRegisters)
				{
					precolored.Add(intToReg.Count);
					regToInt[register] = (uint)intToReg.Count;
					intToReg.Add(register);
				}

				initial = getInitialVirtualRegisters();

				foreach (uint r in initial)
				{
					maxRegNum = (int)r > maxRegNum ? (int)r : maxRegNum;
//					virtualRegisteWeight.Add(intToReg[(int)r], 50);
				}

				foreach (int r in precolored)
				{
					maxRegNum = r > maxRegNum ? r : maxRegNum;
//					virtualRegisteWeight.Add(intToReg[r], int.MaxValue);
				}

				if (inputRegisterWeight != null)
					foreach (VirtualRegister r in inputRegisterWeight.Keys)
//						virtualRegisteWeight[r] = inputRegisterWeight[r];

				callerSavesRegister = new BitVector();

				foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
					callerSavesRegister.Add((int)regToInt[register]);

				// END NOTE.

				allocCount++;

				maxInstNr = 0;

				foreach (SpuBasicBlock block in basicBlocks)
					for (SpuInstruction inst = block.Head; inst != null; inst = inst.Next)
					{
						maxInstNr++;
						intToSpuInst.Add(inst);
						spuinstToInt[inst] = maxInstNr-1;
					}

				selectedForSpill = new BitVector();

				pred = new BitVector[maxInstNr];
				succ = new BitVector[maxInstNr];

				liveIn = new BitVector[maxInstNr];
				liveOut = new BitVector[maxInstNr];

//				degree.Clear();

//				adjSet.Clear();
				adjMatrix.Clear();

				int instNr = 0;
				foreach (SpuBasicBlock block in basicBlocks)
				{
					SpuInstruction inst = block.Head;

					while (inst != null)
					{
						instNr++;

						pred[instNr-1] = new BitVector();
						succ[instNr-1] = new BitVector();

						liveIn[instNr-1] = new BitVector();
						liveOut[instNr-1] = new BitVector();

						inst = inst.Next;
					}
				}

				moveList = new BitVector[maxRegNum + 1];
				adjList = new BitVector[maxRegNum + 1];

				foreach (uint r in initial)
				{
					adjList[r] = new BitVector();
					moveList[r] = new BitVector();
				}

				foreach (int r in precolored)
				{
					adjList[(uint) r] = new BitVector();
					moveList[(uint)r] = new BitVector();
				}

				degree = new int[maxRegNum+1];

				color = new CellRegister[maxRegNum + 1];

				alias = new uint[maxRegNum+1];

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

				foreach (VirtualRegister register in HardwareRegister.VirtualHardwareRegisters)
				{
					color[regToInt[register]] = register.Register;
				}

				LivenessAnalysis();
				Build();

				MakeWorklist();

				do
				{
					allocLoopCount++;

					// DEBUG output

//					Console.WriteLine("Degrees.");
//					foreach (VirtualRegister r in regToInt.Keys)
//					{
//						if (!precolored.Contains((int)regToInt[r]))
//							Console.Write("{0}: {1} ", r, degree[regToInt[r]]);
//					}
//					Console.WriteLine();
//
//					Console.WriteLine("SimplifyWorklist:");
//					foreach (uint u in SimplifyWorklist)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("FreezeWorklist:");
//					foreach (uint u in FreezeWorklist)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("SpillWorklist:");
//					foreach (uint u in SpillWorklist)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("SpilledNodes:");
//					foreach (uint u in SpilledNodes)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("CoalescedNodes:");
//					foreach (int u in CoalescedNodes)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("ColoredNodes:");
//					foreach (int u in ColoredNodes)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("selectStack:");
//					foreach (uint u in selectStack)
//						Console.Write(intToReg[(int)u] + " ");
//					Console.WriteLine();
//
//
//					Console.WriteLine("coalescedMoves:");
//					foreach (int i in coalescedMoves)
//						Console.Write(intToSpuInst[i] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("constrainedMoves:");
//					foreach (int i in constrainedMoves)
//						Console.Write(intToSpuInst[i] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("frozenMoves:");
//					foreach (int i in frozenMoves)
//						Console.Write(intToSpuInst[i] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("worklistMoves:");
//					foreach (int i in worklistMoves)
//						Console.Write(intToSpuInst[i] + " ");
//					Console.WriteLine();
//
//					Console.WriteLine("activeMoves:");
//					foreach (int i in activeMoves)
//						Console.Write(intToSpuInst[i] + " ");
//					Console.WriteLine();

//					using (StreamWriter writer = new StreamWriter("adjMatrix.txt", false, Encoding.ASCII))
//					{
//						writer.WriteLine(adjMatrix.PrintFullMatrix());
//					}

					// TESTS =========================================

					// Test: adjMatrix == adjList
					for (int i = 0; i < adjList.Length; i++)
						if (!intToReg[i].IsRegisterSet)
							Utilities.Assert(adjList[i].Equals(adjMatrix.GetRow(i)), "AjdList is not equals AjdMatrix");

					// Test: adjMatris is symetric
					Utilities.Assert(adjMatrix.IsSymetric(), "adjmatrix is not symetric");

					// Test: Invariants
					InvariantsTest();

					TestNodeSetConsistent();

					// TESTS =========================================

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
					if(inst.OpCode == SpuOpCode.move)
					{
						if (inst.Ra == inst.Rt)
						{
							if (inst.Prev != null)
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
						else
						{
							//TODO brug Ori som move
							// rt, ra, const
//							WriteOri(dest, src, 0);

							SpuInstruction instOri = new SpuInstruction(SpuOpCode.ori);
							instOri.Ra = inst.Ra;
							instOri.Rt = inst.Rt;
							instOri.Constant = 0;

							instOri.Next = inst.Next;
							instOri.Prev = inst.Prev;
							
							if(inst.Prev != null)
								inst.Prev.Next = instOri;
							else
								block.Head = instOri;

							if (inst.Next != null)
								inst.Next.Prev = instOri;

//							SpuInstruction inst1 = new SpuInstruction(SpuOpCode.il);
//							inst1.Rt = HardwareRegister.GetHardwareRegister(79);
//							inst1.Constant = 0;
//
//							SpuInstruction inst2 = new SpuInstruction(SpuOpCode.or);
//							inst2.Rt = inst.Def;
//							inst2.Ra = inst.Use[0];
//							inst2.Rb = inst1.Rt;
//
//							inst1.Next = inst2;
//							inst2.Prev = inst1;
//
//							inst1.Prev = inst.Prev;
//							inst2.Next = inst.Next;
//
//							if (inst.Prev != null)
//							{
//								inst.Prev.Next = inst1;
//							}
//							else
//							{
//								block.Head = inst1;
//							}
//
//							if(inst.Next != null)
//							{
//								inst.Next.Prev = inst2;
//							}
						}
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
						uint val;
						if(!regToInt.TryGetValue(inst.Ra, out val))
						{
							regToInt[inst.Ra] = (uint)intToReg.Count;
							intToReg.Add(inst.Ra);

							result.Add((int)regToInt[inst.Ra]);
						}
					}
					if (inst.Rb != null && !inst.Rb.IsRegisterSet)
					{
						uint val;
						if (!regToInt.TryGetValue(inst.Rb, out val))
						{
							regToInt[inst.Rb] = (uint) intToReg.Count;
							intToReg.Add(inst.Rb);

							result.Add((int) regToInt[inst.Rb]);
						}
					}
					if (inst.Rc != null && !inst.Rc.IsRegisterSet)
					{
						uint val;
						if (!regToInt.TryGetValue(inst.Rc, out val))
						{
							regToInt[inst.Rc] = (uint) intToReg.Count;
							intToReg.Add(inst.Rc);

							result.Add((int) regToInt[inst.Rc]);
						}
					}
					if (inst.Rt != null && !inst.Rt.IsRegisterSet)
					{
						uint val;
						if (!regToInt.TryGetValue(inst.Rt, out val))
						{
							regToInt[inst.Rt] = (uint) intToReg.Count;
							intToReg.Add(inst.Rt);

							result.Add((int) regToInt[inst.Rt]);
						}
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


			foreach (SpuBasicBlock bb in basicBlocks)
				jumpSources[bb] = new LinkedList<SpuInstruction>();

			SpuInstruction predecessor = null;

			// Building predecessore og successor
//			foreach (SpuBasicBlock bb in basicBlocks)
//			{
//				SpuInstruction inst = bb.Head;
//
//				jumpTargets[bb] = inst;
//
//				while (inst != null)
//				{
//					if (predecessor != null)
//					{
//						succ[predecessor].Add(inst);
//						pred[inst].Add(predecessor);
//					}
//					if (inst.JumpTarget != null)
//						jumpSources[inst.JumpTarget].AddLast(inst);
//					predecessor = inst;
//					inst = inst.Next;
//				}
//			}

			int instNr = 0;

			foreach (SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				if (inst != null)
				{
					jumpTargets.Add(bb, inst);
				}
				while (inst != null)
				{
					if (predecessor != null)
					{
						succ[instNr-1].Add(instNr);
						pred[instNr].Add(instNr-1);
					}
					if (inst.JumpTarget != null)
						jumpSources[inst.JumpTarget].AddLast(inst);
					predecessor = inst;
					inst = inst.Next;
					instNr++;
				}
			}

			// Patch predecessore og successor with jump info
			foreach (KeyValuePair<SpuBasicBlock, SpuInstruction> pair in jumpTargets)
			{
				SpuBasicBlock bb = pair.Key;
				SpuInstruction i = pair.Value;

				int iNr = spuinstToInt[i];
				foreach (SpuInstruction s in jumpSources[bb])
				{
					int sNr = spuinstToInt[s];
					succ[sNr].Add(iNr);
					pred[iNr].Add(sNr);
				}
			}

			// Calculate live info.
			// Initialize liveIn and liveOut.
//			foreach (SpuBasicBlock bb in basicBlocks)
//				for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
//				{
//					liveIn[inst] = new BitVector();
//					liveOut[inst] = new BitVector();
//				}

			for(int i=0; i < maxInstNr; i++)
			{
				liveIn[i] = new BitVector();
				liveOut[i] = new BitVector();
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



//				for(int i = basicBlocks.Count-1; i >= 0; i--)
//				{
//					SpuBasicBlock bb = basicBlocks[i];
//					SpuInstruction lastinst = null;
//					for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
//						lastinst = inst;
//
//					for (SpuInstruction inst = lastinst; inst != null; inst = inst.Prev)
//					{
//						BitVector oldLiveIn = new BitVector();
//						oldLiveIn.AddAll(liveIn[inst]);
//
//						BitVector oldLiveOut = new BitVector();
//						oldLiveOut.AddAll(liveOut[inst]);
//
//						liveOut[inst] = new BitVector();
//						foreach (SpuInstruction s in succ[inst])
//							liveOut[inst].AddAll(liveIn[s]);
//
//						liveIn[inst] = new BitVector();
//						liveIn[inst].AddAll(liveOut[inst]);
//						if (inst.Def != null)
//							liveIn[inst].Remove((int) regToInt[inst.Def]);
////						liveIn[inst].AddAll(inst.Use);
//						foreach (VirtualRegister register in inst.Use)
//							liveIn[inst].Add((int) regToInt[register]);
//
//
//						if (!reIterate)
//							reIterate |= !oldLiveIn.Equals(liveIn[inst]) || !oldLiveOut.Equals(liveOut[inst]);
//					}
//				}

				for(int i = maxInstNr-1; i >= 0; i--)
				{
					BitVector oldLiveIn = new BitVector();
					oldLiveIn.AddAll(liveIn[i]);

					BitVector oldLiveOut = new BitVector();
					oldLiveOut.AddAll(liveOut[i]);

					liveOut[i] = new BitVector();
					foreach (int s in succ[i])
						liveOut[i].AddAll(liveIn[s]);

					if(i == maxInstNr-1)
						liveOut[maxInstNr - 1].AddAll(callerSavesRegister);

					liveIn[i] = new BitVector();
					liveIn[i].AddAll(liveOut[i]);

					SpuInstruction inst = intToSpuInst[i];

					if (inst.Def != null)
						liveIn[i].Remove((int)regToInt[inst.Def]);
					//						liveIn[inst].AddAll(inst.Use);

					if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MethodCall) != 0)
						foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
							liveIn[i].Remove((int) regToInt[register]);

					foreach (VirtualRegister register in inst.Use)
						liveIn[i].Add((int)regToInt[register]);

					if (!reIterate)
						reIterate |= !oldLiveIn.Equals(liveIn[i]) || !oldLiveOut.Equals(liveOut[i]);
				}

			} while (reIterate);
		}

		private void Build()
		{
//			foreach (SpuBasicBlock bb in basicBlocks)
//			{
//				for (SpuInstruction inst = bb.Head; inst != null; inst = inst.Next)
//				{
//					BitVector live = new BitVector();
//					live.AddAll(liveOut[inst]);
//					if (inst.OpCode != null && inst.OpCode == SpuOpCode.move)
//					{
////						live.RemoveAll(inst.Use);
//						foreach (VirtualRegister register in inst.Use)
//							live.Remove((int) regToInt[register]);
//
//						moveList[regToInt[inst.Def]].Add(inst);
//						moveList[regToInt[inst.Use[0]]].Add(inst); // Move instruction only have one use register.
//						worklistMoves.Add(inst);
//					}
//					if (inst.Def != null)
//					{
//						live.Add((int) regToInt[inst.Def]);
//						foreach (int l in live)
//							AddEdge((uint) l, regToInt[inst.Def]);
//					}
//				}
//			}


			for (int i = 0; i < maxInstNr; i++)
			{
				BitVector live = new BitVector();
				live.AddAll(liveOut[i]);

				SpuInstruction inst = intToSpuInst[i];

				if (inst.OpCode != null && inst.OpCode == SpuOpCode.move)
				{
					//						live.RemoveAll(inst.Use);
					foreach (VirtualRegister register in inst.Use)
						live.Remove((int) regToInt[register]);

					moveList[regToInt[inst.Def]].Add(i);
					moveList[regToInt[inst.Use[0]]].Add(i); // Move instruction only have one use register.
					worklistMoves.Add(i);
				}
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MethodCall) != 0)
				{
					live.AddAll(callerSavesRegister);

					foreach (int creg in callerSavesRegister)
						foreach (int lreg in live)
							AddEdge((uint) creg, (uint) lreg);
				}
				else if (inst.Def != null)
				{
					uint regdef = regToInt[inst.Def];

					live.Add((int)regdef);

					foreach (int l in live)
						AddEdge((uint)l, regdef);
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
			initial.Clear();
		}

		private BitVector Adjacent(uint r)
		{
			BitVector result = new BitVector();
			result.AddAll(adjList[r]);
			result.RemoveAll(selectStackBitVector);
			result.RemoveAll(coalescedNodes);
			return result;
		}

		private BitVector	NodeMoves(uint r)
		{
			BitVector result = (activeMoves | worklistMoves);
			result.And(moveList[r]);
			return result;
		}

		private bool MoveRelated(uint r)
		{
//			return NodeMoves(r).Count != 0;
			return !NodeMoves(r).IsCountZero();
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
				EnableMoveOptimized((uint)r);
		}

		private void EnableMove(uint r)
		{
			foreach (int i in NodeMoves(r))
				if (activeMoves.Contains(i))
				{
					activeMoves.Remove(i);
					worklistMoves.Add(i);
				}
		}

		private void EnableMoveOptimized(uint r)
		{
			// First optimized implementation
//			BitVector v = NodeMoves(r);
//			v.And(activeMoves);
//			activeMoves.RemoveAll(v);
//			worklistMoves.AddAll(v);

			//Second optimized implementation.
			activeMoves.RemoveAllAnd(moveList[r], worklistMoves);
			worklistMoves.AddAllAnd(activeMoves, moveList[r]);

			//Original implementation
//			foreach (int i in NodeMoves(r))
//				if (activeMoves.Contains(i))
//				{
//					activeMoves.Remove(i);
//					worklistMoves.Add(i);
//				}
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
			Utilities.Assert(!worklistMoves.IsCountZero(), "!worklistMoves.IsCountZero()");

			int move = (int) worklistMoves.getItem();

			SpuInstruction moveInst = intToSpuInst[move];

			uint x = GetAlias(regToInt[moveInst.Use[0]]);
			uint y = GetAlias(regToInt[moveInst.Def]);

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
			if (freezeWorklist.Contains(v))
			{
				//TODO LinkedList not optimal.
				Utilities.Assert(freezeWorklist.Contains(v), "Register allocation error.");
				freezeWorklist.Remove(v); //TODO LinkedList not optimal.
			}
			else
			{
				Utilities.Assert(spillWorklist.Contains(v), "Register allocation error.");
				spillWorklist.Remove(v);
			}

			coalescedNodes.Add((int) v);
			alias[v] = u;
			moveList[u].AddAll(moveList[v]);
			EnableMoveOptimized(v);
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
			foreach (int move in NodeMoves(u))
			{
				SpuInstruction moveInst = intToSpuInst[move];

				uint x = regToInt[moveInst.Def];
				uint y = regToInt[moveInst.Use[0]];

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
			foreach (uint register in spillWorklist)
			{
				if (virtualRegisteWeight[intToReg[(int) register]] <= 10)
				{
					spillWorklist.Remove(register);
					simplifyWorklist.AddLast(register);
					FreezeMoves(register);
					selectedForSpill.Add((int) register);
					return;
				}
			}

			foreach (uint register in spillWorklist)
			{
				if (virtualRegisteWeight[intToReg[(int)register]] <= 100)
				{
					spillWorklist.Remove(register);
					simplifyWorklist.AddLast(register);
					FreezeMoves(register);
					selectedForSpill.Add((int) register);
					return;
				}
			}

			throw new RegisterAllocationException("Unable to spill.");
		}

		private void AssignColors()
		{
			BitVector initOkColors = new BitVector(HardwareRegister.GetCallerSavesCellRegisters().Length + HardwareRegister.GetCalleeSavesCellRegisters().Length);

			foreach (CellRegister register in HardwareRegister.GetCallerSavesCellRegisters())
				initOkColors.Add((int)register);

			foreach (CellRegister register in HardwareRegister.GetCalleeSavesCellRegisters())
				initOkColors.Add((int)register);

			while (selectStack.Count > 0)
			{
				uint n = selectStack.Pop();
				selectStackBitVector.Remove((int) n);

//				Set<CellRegister> okColors = new Set<CellRegister>(HardwareRegister.GetCallerSavesCellRegisters().Length + HardwareRegister.GetCalleeSavesCellRegisters().Length);
//				okColors.AddAll(HardwareRegister.GetCallerSavesCellRegisters());
//				okColors.AddAll(HardwareRegister.GetCalleeSavesCellRegisters());

				BitVector okColors = new BitVector(initOkColors);

				foreach (int w in adjList[n])
				{
					uint a = GetAlias((uint) w);

					if (coloredNodes.Contains((int) a) || precolored.Contains((int) a))
						okColors.Remove((int)color[a]);
				}
				if (okColors.Count <= 0)
				{
					spilledNodes.Add((int) n);
				}
				else
				{
					coloredNodes.Add((int) n);
					CellRegister c = (CellRegister) okColors.getItem();
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

				regToInt.Remove(v);
				Utilities.Assert(_StackSpaceAllocator != null, "Unable to spill.");

				int spillOffset = _StackSpaceAllocator(1);

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
							maxRegNum++;

							virtualRegisteWeight[vt] = int.MaxValue;
							newTemps.Add(regToInt[vt]);

							inst.Rt = vt;

							SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);

							stor.Rt = vt;
							stor.Constant = spillOffset;
							stor.Ra = HardwareRegister.SP;

							SpuInstruction next = inst.Next;
							inst.Next = stor;
							stor.Prev = inst;
							stor.Next = next;
							if (next != null)
								next.Prev = stor;

							prevInst = stor;
							inst = prevInst.Next;
						}
						if (inst.Ra == v || inst.Rb == v || inst.Rc == v || (inst.Rt == v && inst.Def != v))
						{
							VirtualRegister vt = new VirtualRegister();

							regToInt[vt] = (uint)intToReg.Count;
							intToReg.Add(vt);
							maxRegNum++;

							virtualRegisteWeight[vt] = int.MaxValue;
							newTemps.Add(regToInt[vt]);

							if (inst.Ra == v)
								inst.Ra = vt;
							if (inst.Rb == v)
								inst.Rb = vt;
							if (inst.Rc == v)
								inst.Rc = vt;
							if (inst.Rt == v && inst.Def != v)
								inst.Rt = vt;

							SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

							load.Rt = vt;
							load.Constant = spillOffset;
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

			foreach (uint r in newTemps)
			{
				initial.AddLast(r);
			}

		}

		private String BitToReg(BitVector v)
		{
			StringBuilder text = new StringBuilder();

			foreach (int i in v)
			{
				text.Append(intToReg[i]);
				text.Append(" ");
			}
			return text.ToString();
		}

		private void InvariantsTest()
		{
			foreach (KeyValuePair<VirtualRegister, uint> pair in regToInt)
			{
				//Degree invariant
				if (simplifyWorklist.Contains(pair.Value) || freezeWorklist.Contains(pair.Value) || spillWorklist.Contains(pair.Value))
				{
					BitVector v = new BitVector();

					v.AddAll(precolored);
					v.AddAll(simplifyWorklist);
					v.AddAll(freezeWorklist);
					v.AddAll(spillWorklist);
					v.And(adjList[pair.Value]);

					Utilities.Assert(degree[pair.Value] == v.Count, "degree[pair.Value] == v.Count");
				}

				// Simplify worklist invariant
				if (simplifyWorklist.Contains(pair.Value) && !selectedForSpill.Contains((int) pair.Value))
				{
					BitVector v = new BitVector();
					v.AddAll(activeMoves);
					v.AddAll(worklistMoves);
					v.And(moveList[pair.Value]);
					Utilities.Assert(degree[pair.Value] < K && v.IsCountZero(), "degree[pair.Value] < K && v.IsCountZero()");
				}

				// Freeze worklist invariant
				if (freezeWorklist.Contains(pair.Value))
				{
					BitVector v = new BitVector();
					v.AddAll(activeMoves);
					v.AddAll(worklistMoves);
					v.And(moveList[pair.Value]);
					Utilities.Assert(degree[pair.Value] < K && !v.IsCountZero(), "degree[pair.Value] < K && !v.IsCountZero()");
				}

				// Spill worklist invariant
				Utilities.Assert(!spillWorklist.Contains(pair.Value) || degree[pair.Value] >= K, "!SpillWorklist.Contains(pair.Value) || degree[pair.Value] >= K");
			}
		}

		private void MoveReg(RegWorklist fromSet, RegWorklist toSet, int reg)
		{
			switch (fromSet)
			{
				case RegWorklist.Precolored:
					throw new RegisterAllocationException("Not possible to remove node from precolored.");
				case RegWorklist.Initial:
					initial.Remove((uint)reg);
					break;
				case RegWorklist.SimplifyWorklist:
					simplifyWorklist.Remove((uint) reg);
					break;
				case RegWorklist.FreezeWorklist:
					freezeWorklist.Remove((uint) reg);
					break;
				case RegWorklist.SpillWorklist:
					spillWorklist.Remove((uint) reg);
					break;
				case RegWorklist.SpilledNodes:
					spilledNodes.Remove(reg);
					break;
				case RegWorklist.CoalescedNodes:
					coalescedNodes.Remove(reg);
					break;
				case RegWorklist.ColoredNodes:
					coloredNodes.Remove(reg);
					break;
				case RegWorklist.SelectStack:
					if (selectStack.Peek() == reg)
					{
						selectStack.Pop();
						selectStackBitVector.Remove(reg);
					}
					else
						throw new RegisterAllocationException("Not able to remove node from SelectStack.");
					break;
				default:
					break;
			}

			switch (toSet)
			{
				case RegWorklist.Precolored:
					throw new RegisterAllocationException("Not posible to remove node from precolored.");
				case RegWorklist.Initial:
					initial.AddLast((uint)reg);
					break;
				case RegWorklist.SimplifyWorklist:
					simplifyWorklist.AddLast((uint) reg);
					break;
				case RegWorklist.FreezeWorklist:
					freezeWorklist.AddLast((uint) reg);
					break;
				case RegWorklist.SpillWorklist:
					spillWorklist.AddLast((uint) reg);
					break;
				case RegWorklist.SpilledNodes:
					spilledNodes.Add(reg);
					break;
				case RegWorklist.CoalescedNodes:
					coalescedMoves.Add(reg);
					break;
				case RegWorklist.ColoredNodes:
					coloredNodes.Add(reg);
					break;
				case RegWorklist.SelectStack:
					selectStack.Push((uint)reg);
					selectStackBitVector.Add(reg);
					break;
				default:
					break;
			}
			int count = 0;

			count += precolored.Contains(reg) ? 1 : 0;
//			count += initial.Contains((uint)reg) ? 1 : 0;
			count += simplifyWorklist.Contains((uint)reg) ? 1 : 0;
			count += freezeWorklist.Contains((uint)reg) ? 1 : 0;
			count += spillWorklist.Contains((uint)reg) ? 1 : 0;
			count += spilledNodes.Contains(reg) ? 1 : 0;
			count += coalescedNodes.Contains(reg) ? 1 : 0;
			count += coloredNodes.Contains(reg) ? 1 : 0;
			count += selectStack.Contains((uint)reg) ? 1 : 0;

			Utilities.Assert(count == 1, "count == 1");
		}

		private void TestNodeSetConsistent()
		{
			for(int reg = 0; reg <= maxRegNum; reg++)
			{
				int count = 0;

				count += precolored.Contains(reg) ? 1 : 0;
				count += initial.Contains((uint)reg) ? 1 : 0;
				count += simplifyWorklist.Contains((uint)reg) ? 1 : 0;
				count += freezeWorklist.Contains((uint)reg) ? 1 : 0;
				count += spillWorklist.Contains((uint)reg) ? 1 : 0;
				count += spilledNodes.Contains(reg) ? 1 : 0;
				count += coalescedNodes.Contains(reg) ? 1 : 0;
				count += coloredNodes.Contains(reg) ? 1 : 0;
				count += selectStack.Contains((uint)reg) ? 1 : 0;

				if (regToInt.ContainsKey(intToReg[reg]))
				{
					Utilities.Assert(count == 1, "count == 1");
				}
				else
				{
					Utilities.Assert(count == 0, "count == 0");
				}
			}
		}

		private enum RegWorklist
		{
			Precolored, Initial, SimplifyWorklist, FreezeWorklist, SpillWorklist, SpilledNodes, CoalescedNodes, ColoredNodes, SelectStack
		}

		public void DumpStateToFile(String filename)
		{
			using (StreamWriter writer = new StreamWriter(filename, false, Encoding.ASCII))
			{
				writer.WriteLine("intToReg:");
				for (int i = 0; i < intToReg.Count; i++)
					writer.Write("{0} {1} ", i, intToReg[i]);
				writer.WriteLine();

				writer.WriteLine("precolored:");
				writer.WriteLine(precolored);

				writer.WriteLine("Initial:");
				foreach (uint u in initial)
					writer.Write("{0} ", u);
				writer.WriteLine();

				writer.WriteLine("SimplifyWorklist:");
				foreach (uint u in simplifyWorklist)
					writer.Write("{0} ", u);
				writer.WriteLine();

				writer.WriteLine("FreezeWorklist:");
				foreach (uint u in freezeWorklist)
					writer.Write("{0} ", u);
				writer.WriteLine();

				writer.WriteLine("SpillWorklist:");
				foreach (uint u in spillWorklist)
					writer.Write("{0} ", u);
				writer.WriteLine();

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(spilledNodes);

				writer.WriteLine("CoalescedNodes:");
				writer.WriteLine(coalescedNodes);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(coloredNodes);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(selectStack);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(coalescedMoves);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(constrainedMoves);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(frozenMoves);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(worklistMoves);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(activeMoves);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(adjMatrix);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(adjList);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(degree);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(moveList);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(alias);

				writer.WriteLine("SpilledNodes:");
				writer.WriteLine(color);
				writer.WriteLine();
			}
		}
	}

}
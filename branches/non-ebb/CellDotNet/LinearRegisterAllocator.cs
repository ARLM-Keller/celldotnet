using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	/// <summary>
	/// A simple linear scan register allocator based on http://www.research.ibm.com/jalapeno/papers/toplas99.pdf.
	/// </summary>
	internal class LinearRegisterAllocator
	{
		// TODO Registre allokatoren bør arbejde på hele metoden.
		// returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
		// regnum, er altallet af registre som register allokatoren ikke bruger.

		private class RegisterPool
		{
			/// <summary>
			/// The registers that currently are available for use.
			/// </summary>
			private SortedRegisterSet _availableRegisters;
//			private Set<CellRegister> _availableRegisters;

			/// <summary>
			/// All registers that have been returned by <see cref="GetFreeRegister"/>.
			/// </summary>
			private Set<CellRegister> _usedRegisters;

			private Set<CellRegister> _usedPrecoloredRegisters;


			class SortedRegisterSet
			{
				class RegNumComparer : IComparer<CellRegister>
				{
					public int Compare(CellRegister x, CellRegister y)
					{
						return y - x;
					}

					public static RegNumComparer Inverse = new RegNumComparer();
				}

				List<CellRegister> _list = new List<CellRegister>();


				public void Add(CellRegister reg)
				{
					_list.Add(reg);
					_list.Sort(RegNumComparer.Inverse);
				}

				public CellRegister GetSome()
				{
					CellRegister reg = _list[_list.Count - 1];
					_list.RemoveAt(_list.Count - 1);

					return reg;
				}

				public bool Contains(CellRegister reg)
				{
					return _list.Contains(reg);
				}

				public void Remove(CellRegister register)
				{
					int index = _list.FindIndex(delegate(CellRegister obj) { return obj == register; });
					if (index == -1)
						throw new ArgumentException("Register " + register + " is not in the set.");

					_list.RemoveAt(index);
				}
			}

			public RegisterPool()
			{
				_usedRegisters = new Set<CellRegister>();
				_usedPrecoloredRegisters = new Set<CellRegister>();

				// Only use calle-saves.
				_availableRegisters = new SortedRegisterSet();
				foreach (CellRegister regnum in HardwareRegister.GetCalleeSavesCellRegisters())
				{
					_availableRegisters.Add(regnum);
				}
			}

			public List<VirtualRegister> GetAllUsedCalleeSavesRegisters()
			{
				// Currently they're all callee, so return everything.
				List<VirtualRegister> l = new List<VirtualRegister>();
				foreach (CellRegister cr in _usedRegisters)
				{
					l.Add(HardwareRegister.GetHardwareRegister(cr));
				}

				// Just to make the code look nicer.
				l.Sort(delegate(VirtualRegister x, VirtualRegister y) { return x.Register - y.Register; });

				return l;
			}

			public CellRegister GetFreeRegister()
			{
				CellRegister reg = _availableRegisters.GetSome();
//				CellRegister reg = Utilities.GetMinimum(_availableRegisters, 
//					delegate(CellRegister x, CellRegister y) { return (int) x - (int) y; });
//				CellRegister reg = Utilities.GetFirst(_availableRegisters);
//				_availableRegisters.Remove(reg);
				_usedRegisters.Add(reg);

				return reg;
			}


			/// <summary>
			/// Tells the pool that the <paramref name="register"/> from now on is used by a precolored register.
			/// <see cref="MarkPrecoloredRegisterEndOfUsage"/> is 
			/// </summary>
			/// <param name="register"></param>
			public void MarkPrecoloredRegisterUsed(CellRegister register)
			{
				if (register >= CellRegister.REG_80)
					Utilities.Assert(_availableRegisters.Contains(register), "_availableRegisters.Contains(register)");
				Utilities.Assert(!_usedRegisters.Contains(register), "!_usedPrecoloredRegisters.Contains(register)");
				Utilities.Assert(!_usedPrecoloredRegisters.Contains(register), "!_usedPrecoloredRegisters.Contains(register)");

				if (register >= CellRegister.REG_80)
					_availableRegisters.Remove(register);

				_usedPrecoloredRegisters.Add(register);
			}

			public void MarkPrecoloredRegisterEndOfUsage(CellRegister register)
			{
				if (register >= CellRegister.REG_80)
				{
					Utilities.Assert(!_availableRegisters.Contains(register), "!_availableRegisters.Contains(register)");
					Utilities.Assert(_usedRegisters.Contains(register), "_usedPrecoloredRegisters.Contains(register)");
				}
				Utilities.Assert(_usedPrecoloredRegisters.Contains(register), "_usedPrecoloredRegisters.Contains(register)");

				bool gotremoved = _usedPrecoloredRegisters.Remove(register);
				Utilities.Assert(gotremoved, "gotremoved");

				if (register >= CellRegister.REG_80)
					_availableRegisters.Add(register);
			}

			public void AddFreeRegister(CellRegister register)
			{
				Utilities.AssertArgument(!_availableRegisters.Contains(register), "!_availableRegisters.Contains(register)");

				if (_usedPrecoloredRegisters.Contains(register))
					MarkPrecoloredRegisterEndOfUsage(register);
				else
					_availableRegisters.Add(register);
			}
		}

		/// <summary>
		/// A set of intervals that is sorted by interval end.
		/// </summary>
		private class ActiveIntervalSet
		{
			/// <summary>
			/// The head is the one that with the earliest end.
			/// </summary>
			private List<LiveInterval> _list;

			public ActiveIntervalSet()
			{
				_list = new List<LiveInterval>();
			}

			public void Add(LiveInterval interval)
			{
				_list.Add(interval);
				_list.Sort(LiveInterval.CompareByEnd.Instance);
			}

			public int Count
			{
				get { return _list.Count; }
			}

			public LiveInterval PeekStart()
			{
				return _list[0];
			}

			public LiveInterval RemoveStart()
			{
				if (_list.Count == 0)
					throw new InvalidOperationException();

				LiveInterval i = _list[0];
				_list.RemoveAt(0);

				return i;
			}
		}

		private RegisterPool _registerPool;
		private ActiveIntervalSet _active;

		public void Allocate(List<SpuBasicBlock> spuBasicBlocks,
		                     StackSpaceAllocator inputStackSpaceAllocator, SpuBasicBlock innerEpilog)
		{
			_registerPool = new RegisterPool();
			_active = new ActiveIntervalSet();

			List<LiveInterval> intlist = CreateSortedLiveIntervals(spuBasicBlocks);

			foreach (LiveInterval interval in intlist)
			{
				ExpireOldIntervals(interval);

				// Leave precolored registers.
				if (interval.VirtualRegister.IsRegisterSet)
				{
					_registerPool.MarkPrecoloredRegisterUsed(interval.VirtualRegister.Register);
				}
				else if (_active.Count > 46)
				{
					SpillAtInterval(interval);
				}
				else
				{
					// Assign new register.
					Utilities.Assert(!interval.VirtualRegister.IsRegisterSet, "!interval.VirtualRegister.IsRegisterSet");
					interval.VirtualRegister.Register = _registerPool.GetFreeRegister();
					_active.Add(interval);
				}
			}


			// Store callee saves.
			List<VirtualRegister> usedCallee = _registerPool.GetAllUsedCalleeSavesRegisters();
			List<KeyValuePair<VirtualRegister, int>> calleesWithOffset = new List<KeyValuePair<VirtualRegister, int>>();
			SpuInstructionWriter storeWriter = new SpuInstructionWriter();
			storeWriter.BeginNewBasicBlock();
			for (int i = 0; i < usedCallee.Count; i++)
			{
				VirtualRegister hwreg = usedCallee[i];
				Utilities.Assert(hwreg.IsRegisterSet, "hwreg.IsRegisterSet");

				KeyValuePair<VirtualRegister, int> entry = new KeyValuePair<VirtualRegister, int>(hwreg, inputStackSpaceAllocator(1));

				calleesWithOffset.Add(entry);
				storeWriter.WriteStqd(hwreg, HardwareRegister.SP, entry.Value);
			}
			spuBasicBlocks.Insert(0, storeWriter.CurrentBlock);


			// Load callee saves.
			SpuInstructionWriter loadWriter = new SpuInstructionWriter();
			if(innerEpilog == null)
				loadWriter.BeginNewBasicBlock();
			else
				loadWriter.AppendBasicBlock(innerEpilog);

			foreach (KeyValuePair<VirtualRegister, int> pair in calleesWithOffset)
			{
				VirtualRegister hwreg = pair.Key;
				int offset = pair.Value;

				loadWriter.WriteLqd(hwreg, HardwareRegister.SP, offset);
			}
			spuBasicBlocks.Add(loadWriter.CurrentBlock);

			RegAllocGraphColloring.RemoveRedundantMoves(spuBasicBlocks);
		}

		internal static List<LiveInterval> CreateSortedLiveIntervals(List<SpuBasicBlock> spuBasicBlocks)
		{
			Set<VirtualRegister>[] liveIn;
			Set<VirtualRegister>[] liveOut;
			IterativeLivenessAnalyser.Analyze(spuBasicBlocks, out liveIn, out liveOut, true);


//			PrintLiveOut(liveOut, spuBasicBlocks);

			List<LiveInterval> intlist = new List<LiveInterval>(GenerateLiveIntervals(liveOut).Values);
//			intlist.Sort(LiveInterval.CompareByStart.Instance);

			// This "advanced" sort is simply to get nicer prints.
			intlist.Sort(delegate(LiveInterval x, LiveInterval y)
			             	{
			             		int diff = LiveInterval.CompareByStart.Instance.Compare(x, y);
			             		if (diff != 0)
			             			return diff;

			             		return (x.VirtualRegister.IsRegisterSet ? x.VirtualRegister.Register : 0) -
			             		       (y.VirtualRegister.IsRegisterSet ? y.VirtualRegister.Register : 0);
			             	});

//			PrintLiveIntervals(liveOut.Length, intlist);
			return intlist;
		}

		private static void PrintLiveOut(Set<VirtualRegister>[] liveOut, List<SpuBasicBlock> spuBasicBlocks)
		{
			List<SpuInstruction> instlist = new List<SpuInstruction>();
			foreach (SpuBasicBlock bb in spuBasicBlocks)
			{
				instlist.AddRange(bb.Head.GetEnumerable());
			}

			StringWriter sw = new StringWriter();
			sw.WriteLine("Live out:");
			for (int i = 0; i < liveOut.Length; i++)
			{
				sw.Write("{0,2} {1}:", i, instlist[i].OpCode.Name);
				foreach (VirtualRegister reg in liveOut[i])
				{
					sw.Write(" " + reg);
				}
				sw.WriteLine();
			}
			Console.WriteLine(sw.GetStringBuilder());
		}

		private static void PrintLiveIntervals(int instcount, List<LiveInterval> intlist)
		{
			StringWriter sw = new StringWriter();
			foreach (LiveInterval interval in intlist)
			{
				int precount = interval.Start;
				int count = (interval.End + 1) - interval.Start;

				sw.Write(interval.VirtualRegister.ToString().PadRight(9));
				sw.Write("({0,3:d} - {1,3:d}) : ", interval.Start, interval.End);
				sw.Write(new string(' ', precount));
				sw.Write(new string('-', count));
				sw.WriteLine();
			}
			Console.WriteLine("Intervals for {0} instructions:", instcount);
			Console.Write(sw.GetStringBuilder());
		}

		private void SpillAtInterval(LiveInterval interval)
		{
			throw new NotImplementedException("Cannot spill. Need to implement SpillAtInterval.");
		}

		private void ExpireOldIntervals(LiveInterval currentInterval)
		{
			while (_active.Count > 0 && _active.PeekStart().End < currentInterval.Start)
			{
				LiveInterval removed = _active.RemoveStart();
				_registerPool.AddFreeRegister(removed.VirtualRegister.Register);
			}
		}

//		public static bool Alloc(List<SpuBasicBlock> spuBasicBlocks,
//		                         RegAllocGraphColloring.NewSpillOffsetDelegate inputNewSpillOffset)
//		{
//			Set<VirtualRegister>[] liveIn;
//			Set<VirtualRegister>[] liveOut;
//
//			IterativeLivenessAnalyser.Analyze(spuBasicBlocks, out liveIn, out liveOut, true);
//			Dictionary<VirtualRegister, LiveInterval> regToLiveDict = GenerateLiveIntervals(liveOut);
////			Dictionary<VirtualRegister, LiveInterval> regToLiveDict = GenerateLiveIntervals(liveOut, null);
//
//			List<LiveInterval> liveIntervals = new List<LiveInterval>();
//
//			SortedLinkedList<SortedLinkedList<LiveInterval>> hardwareIntervals =
//				new SortedLinkedList<SortedLinkedList<LiveInterval>>(new ListComparator());
//
//
//			// Create sorted list of live intervals.
//			foreach (KeyValuePair<VirtualRegister, LiveInterval> pair in regToLiveDict)
//			{
//				if (!pair.Key.IsRegisterSet)
//					liveIntervals.Add(pair.Value);
//			}
//			liveIntervals.Sort(LiveInterval.CompareByStart.Instance);
//
//
//			List<SpuInstruction> code = new List<SpuInstruction>();
//
//			foreach (SpuBasicBlock block in spuBasicBlocks)
//			{
//				SpuInstruction inst = block.Head;
//				while (inst != null)
//				{
//					code.Add(inst);
//					inst = inst.Next;
//				}
//			}
//
//			Set<VirtualRegister> spilledRegister = new Set<VirtualRegister>();
//
//
//			Dictionary<VirtualRegister, SortedLinkedList<LiveInterval>> hardRegToList =
//				new Dictionary<VirtualRegister, SortedLinkedList<LiveInterval>>();
//
//			Dictionary<SortedLinkedList<LiveInterval>, VirtualRegister> ListToHardReg =
//				new Dictionary<SortedLinkedList<LiveInterval>, VirtualRegister>();
//
//			SortedLinkedList<LiveInterval> overlapingHardReg = new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());
//
//			Dictionary<VirtualRegister, LiveInterval> currentIntervals = new Dictionary<VirtualRegister, LiveInterval>();
//
//			foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
//			{
//				hardRegToList[register] = new SortedLinkedList<LiveInterval>(new LiveInterval.CompareByStart());
//				currentIntervals[register] = new LiveInterval();
//				currentIntervals[register].Start = -1;
//				currentIntervals[register].End = -1;
//				currentIntervals[register].r = register;
//			}
//
//			foreach (VirtualRegister register in HardwareRegister.CalleeSavesRegisters)
//			{
//				hardRegToList[register] = new SortedLinkedList<LiveInterval>(new LiveInterval.CompareByStart());
//				currentIntervals[register] = new LiveInterval();
//				currentIntervals[register].Start = -1;
//				currentIntervals[register].End = -1;
//				currentIntervals[register].r = register;
//			}
//
//
//			// Genarate hardware register "intervals"
//			for (int i = 0; i < code.Count; i++)
//			{
//				foreach (VirtualRegister register in code[i].Use)
//				{
//					if (HardwareRegister.CalleeSavesRegisters.Contains(register) ||
//					    HardwareRegister.CallerSavesRegisters.Contains(register))
//					{
//						currentIntervals[register].End = i;
//					}
//				}
//
//				if (code[i].Def != null && (HardwareRegister.CalleeSavesRegisters.Contains(code[i].Def) ||
//				                            HardwareRegister.CallerSavesRegisters.Contains(code[i].Def)))
//				{
//					hardRegToList[code[i].Def].Add(currentIntervals[code[i].Def]);
//					currentIntervals[code[i].Def] = new LiveInterval();
//					currentIntervals[code[i].Def].Start = i;
//					currentIntervals[code[i].Def].End = i;
//					currentIntervals[code[i].Def].r = code[i].Def;
//				}
//
////				if(in)
//			}
//
//			foreach (KeyValuePair<VirtualRegister, LiveInterval> pair in currentIntervals)
//				hardRegToList[pair.Key].Add(pair.Value);
//
//
//			foreach (KeyValuePair<VirtualRegister, SortedLinkedList<LiveInterval>> pair in hardRegToList)
//			{
//				hardwareIntervals.Add(pair.Value);
//				ListToHardReg.Add(pair.Value, pair.Key);
//			}
//
//
//			SortedLinkedList<LiveInterval> activeIntervals =
//				new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());
//
//
////			Stack<CellRegister> freeRegisters = new Stack<CellRegister>();
//
////			foreach (CellRegister register in HardwareRegister.GetCalleeSavesCellRegisters())
////			{
////				freeRegisters.Push(register);
////			}
////			foreach (CellRegister register in HardwareRegister.GetCallerSavesCellRegisters())
////        	{
////        		freeRegisters.Push(register);	
////        	}
//
//			bool isSpill = false;
//
//
//			Set<VirtualRegister> hardwareRegisters = new Set<VirtualRegister>();
//			hardwareRegisters.AddAll(HardwareRegister.VirtualHardwareRegisters);
//
//// 				new LinearRegisterAllocator().hardwareIntervalsToString(hardwareIntervals)
//
//			foreach (LiveInterval interval in liveIntervals)
//			{
//				// ExpireOldIntervals
//				while (activeIntervals.Count > 0 && activeIntervals.Head.End < interval.Start)
//				{
//					LiveInterval li = activeIntervals.RemoveHead();
//
//					SortedLinkedList<LiveInterval> list;
//
//					if (hardRegToList.TryGetValue(HardwareRegister.GetHardwareRegister((int) li.r.Register), out list))
//						hardwareIntervals.Add(list);
//				}
//
//				while (hardwareIntervals.Head != null && hardwareIntervals.Head.Head != null &&
//				       hardwareIntervals.Head.Head.Start <= interval.Start)
//				{
//					SortedLinkedList<LiveInterval> list = hardwareIntervals.RemoveHead();
//					LiveInterval i = list.RemoveHead();
//
//					while (i != null && i.End < interval.Start)
//						i = list.RemoveHead();
//
//					if (i != null)
//						overlapingHardReg.Add(i);
//					else
//						hardwareIntervals.Add(list);
//				}
//
//				while (overlapingHardReg.Head != null && overlapingHardReg.Head.End < interval.Start)
//				{
//					LiveInterval i = overlapingHardReg.RemoveHead();
//
//					SortedLinkedList<LiveInterval> list = hardRegToList[i.r];
//
//					while (list.Head != null && list.Head.End < interval.Start)
//						list.RemoveHead();
//
//					if (list.Head != null && list.Head.Start <= interval.Start)
//						overlapingHardReg.Add(list.Head);
//					else
//						hardwareIntervals.Add(list);
//				}
//
//
////				while (hardwareIntervals.Head != null && hardwareIntervals.Head.Head != null &&
////					hardwareIntervals.Head.Head.End <= interval.Start)
////				{
////					SortedLinkedList<LiveInterval> list = hardwareIntervals.RemoveHead();
////					list.RemoveHead();
////					hardwareIntervals.Add(list);
////				}
//
////				foreach (SortedLinkedList<LiveInterval> list in hardwareIntervals)
////					while (list.Head != null && list.Head.End < interval.Start)
////					{
////						hardwareIntervals.Remove(list);
////						list.RemoveHead();
////						hardwareIntervals.Add(list);
////					}
//
//				bool toBeSpilled = true;
//
//				if (hardwareIntervals.Count != 0)
//				{
//					foreach (SortedLinkedList<LiveInterval> list in hardwareIntervals)
//					{
//						if (list.Head == null || list.Head.Start > interval.End)
//						{
//							interval.r.Register = ListToHardReg[list].Register;
//							hardwareIntervals.Remove(list);
//							toBeSpilled = false;
//							break;
//						}
//					}
//				}
//				if (toBeSpilled)
//				{
//					// SpillAtInterval
//					isSpill = true;
//					LiveInterval spill = activeIntervals.Tail;
//					if (spill != null && spill.End > interval.End)
//					{
//						interval.r.Location = spill.r.Location;
//						spilledRegister.Add(spill.r);
//						activeIntervals.RemoveTail();
//						activeIntervals.Add(interval);
//					}
//					else
//					{
//						spilledRegister.Add(interval.r);
//					}
//				}
//			}
//
//			if (spilledRegister.Count > 0 && inputNewSpillOffset == null)
//				throw new Exception("LinearRegisterAllocator needs to spill, but no stack offset was given.");
//
//			Dictionary<VirtualRegister, int> spillOffset = new Dictionary<VirtualRegister, int>();
//			foreach (VirtualRegister register in spilledRegister)
//				spillOffset.Add(register, inputNewSpillOffset());
//
//			foreach (SpuBasicBlock block in spuBasicBlocks)
//			{
//				SpuInstruction inst = block.Head;
//				SpuInstruction prevInst = null;
//
//				while (inst != null)
//				{
//					if (inst.Def != null && spilledRegister.Contains(inst.Def))
//					{
//						VirtualRegister vt = HardwareRegister.GetHardwareRegister((CellRegister) 79);
//
//						inst.Rt = vt;
//
//						SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);
//
//						stor.Rt = vt;
//						stor.Constant = spillOffset[inst.Def];
//						stor.Ra = HardwareRegister.SP;
//
//						SpuInstruction next = inst.Next;
//						inst.Next = stor;
//						stor.Prev = inst;
//						stor.Next = next;
//						if (next != null)
//							next.Prev = stor;
//					}
//					if (inst.Ra != null && spilledRegister.Contains(inst.Ra))
//					{
//						VirtualRegister vt = HardwareRegister.GetHardwareRegister((CellRegister) 78);
//
//						inst.Ra = vt;
//
//						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);
//
//						load.Rt = vt;
//						load.Constant = spillOffset[inst.Ra];
//						load.Ra = HardwareRegister.SP;
//
//						if (prevInst != null)
//						{
//							prevInst.Next = load;
//							load.Prev = prevInst;
//						}
//						else
//						{
//							block.Head = load;
//						}
//
//						load.Next = inst;
//						inst.Prev = load;
//					}
//					if (inst.Rb != null && spilledRegister.Contains(inst.Rb))
//					{
//						VirtualRegister vt = HardwareRegister.GetHardwareRegister((CellRegister) 77);
//
//						inst.Rb = vt;
//
//						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);
//
//						load.Rt = vt;
//						load.Constant = spillOffset[inst.Rb];
//						load.Ra = HardwareRegister.SP;
//
//						if (prevInst != null)
//						{
//							prevInst.Next = load;
//							load.Prev = prevInst;
//						}
//						else
//						{
//							block.Head = load;
//						}
//
//						load.Next = inst;
//						inst.Prev = load;
//					}
//					if (inst.Rc != null && spilledRegister.Contains(inst.Rc))
//					{
//						VirtualRegister vt = HardwareRegister.GetHardwareRegister((CellRegister) 76);
//
//						inst.Rc = vt;
//
//						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);
//
//						load.Rt = vt;
//						load.Constant = spillOffset[inst.Rc];
//						load.Ra = HardwareRegister.SP;
//
//						if (prevInst != null)
//						{
//							prevInst.Next = load;
//							load.Prev = prevInst;
//						}
//						else
//						{
//							block.Head = load;
//						}
//
//						load.Next = inst;
//						inst.Prev = load;
//					}
//					if (inst.Rt != null && spilledRegister.Contains(inst.Rt))
//					{
//						VirtualRegister vt = HardwareRegister.GetHardwareRegister((CellRegister) 79);
//
//						inst.Rt = vt;
//
//						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);
//
//						load.Rt = vt;
//						load.Constant = spillOffset[inst.Rt];
//						load.Ra = HardwareRegister.SP;
//
//						if (prevInst != null)
//						{
//							prevInst.Next = load;
//							load.Prev = prevInst;
//						}
//						else
//						{
//							block.Head = load;
//						}
//
//						load.Next = inst;
//						inst.Prev = load;
//					}
//
//					prevInst = inst;
//					inst = prevInst.Next;
//				}
//			}
//
//			// To be safe, we insert the VirtualRegister representing the hardware registers.
//			foreach (SpuBasicBlock block in spuBasicBlocks)
//			{
//				SpuInstruction inst = block.Head;
//				while (inst != null)
//				{
//					if (inst.Rt != null && !hardwareRegisters.Contains(inst.Rt))
//					{
//						inst.Rt = HardwareRegister.GetHardwareRegister(inst.Rt.Register);
//					}
//					if (inst.Ra != null && !hardwareRegisters.Contains(inst.Ra))
//					{
//						inst.Ra = HardwareRegister.GetHardwareRegister(inst.Ra.Register);
//					}
//					if (inst.Rb != null && !hardwareRegisters.Contains(inst.Rb))
//					{
//						inst.Rb = HardwareRegister.GetHardwareRegister(inst.Rb.Register);
//					}
//					if (inst.Rc != null && !hardwareRegisters.Contains(inst.Rc))
//					{
//						inst.Rc = HardwareRegister.GetHardwareRegister(inst.Rc.Register);
//					}
//					inst = inst.Next;
//				}
//			}
//			return isSpill;
//		}

		private static Dictionary<VirtualRegister, LiveInterval> GenerateLiveIntervals(Set<VirtualRegister>[] liveOut)
		{
			Dictionary<VirtualRegister, LiveInterval> liveIntervals = new Dictionary<VirtualRegister, LiveInterval>();


			for (int i = 0; i < liveOut.Length; i++)
			{
				foreach (VirtualRegister register in liveOut[i])
				{
					LiveInterval interval;
					if (liveIntervals.TryGetValue(register, out interval))
					{
						// Extend the end of the interval.
						interval.End = i + 1;
//						Console.WriteLine("Extending: {0} at inst no {1}.", register, i);
					}
					else
					{
						// We haven't seen this vreg before; give it an interval.
						interval = new LiveInterval(register);

//						Console.WriteLine("Creating: {0} at inst no {1}.", register, i);

						interval.Start = i;
						interval.End = i + 1;

						liveIntervals.Add(register, interval);
					}
				}
			}

			return liveIntervals;
		}
	}
}

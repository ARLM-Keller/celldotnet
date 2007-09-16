using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// A simple linear scan register allocator based on http://www.research.ibm.com/jalapeno/papers/toplas99.pdf.
	/// </summary>
	internal class SimpleRegAlloc
	{
		// TODO Registre allokatoren bør arbejde på hele metoden.
		// returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
		// regnum, er altallet af registre som register allokatoren ikke bruger.

		class RegisterPool
		{
			/// <summary>
			/// The registers that currently are available for use.
			/// </summary>
			private Set<CellRegister> _availableRegisters;

			public RegisterPool()
			{
				// Only use calle-saves.
				_availableRegisters = new Set<CellRegister>();
				foreach (CellRegister regnum in HardwareRegister.getCalleeSavesCellRegisters())
				{
					_availableRegisters.Add(regnum);
				}
			}
		}

		/// <summary>
		/// A set of intervals that is sorted by interval end.
		/// </summary>
		class ActiveIntervalSet
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
				_list.Sort(LiveInterval.ComparByEnd.Instance);
			}

			public int Count
			{
				get { return _list.Count; }
			}

			public LiveInterval PeekNext()
			{
				return _list[0];
			}
		}

		public bool Allocate(List<SpuBasicBlock> spuBasicBlocks,
						 RegAllocGraphColloring.NewSpillOffsetDelegate inputNewSpillOffset)
		{
			Set<VirtualRegister>[] liveIn;
			Set<VirtualRegister>[] liveOut;

			IterativLivenessAnalyser.Analyse(spuBasicBlocks, out liveIn, out liveOut);
			List<LiveInterval> intlist = new List<LiveInterval>(GenerateLiveIntervals(liveOut).Values);
			intlist.Sort(LiveInterval.ComparByStart.Instance);

			ActiveIntervalSet ai = new ActiveIntervalSet();
			foreach (LiveInterval interval in intlist)
			{
				ExpireOldIntervals(interval);

				if (ai.Count > 46)
				{
					throw new NotImplementedException("Cannot spill.");
				}
				else
				{
//					interval.VirtualRegister
				}
			}


			throw new NotImplementedException();
		}

		private void ExpireOldIntervals(LiveInterval currentInterval)
		{
			
		}

		public static bool Alloc(List<SpuBasicBlock> spuBasicBlocks,
		                         RegAllocGraphColloring.NewSpillOffsetDelegate inputNewSpillOffset)
		{
			Set<VirtualRegister>[] liveIn;
			Set<VirtualRegister>[] liveOut;

			IterativLivenessAnalyser.Analyse(spuBasicBlocks, out liveIn, out liveOut);
			Dictionary<VirtualRegister, LiveInterval> regToLiveDict = GenerateLiveIntervals(liveOut);

			List<LiveInterval> liveIntervals = new List<LiveInterval>();

			SortedLinkedList<SortedLinkedList<LiveInterval>> hardwareIntervals =
				new SortedLinkedList<SortedLinkedList<LiveInterval>>(new ListComparator());


			// Create sorted list of live intervals.
			foreach (KeyValuePair<VirtualRegister, LiveInterval> pair in regToLiveDict)
			{
				if (!pair.Key.IsRegisterSet)
					liveIntervals.Add(pair.Value);
			}
			liveIntervals.Sort(LiveInterval.ComparByStart.Instance);


			List<SpuInstruction> code = new List<SpuInstruction>();

			foreach (SpuBasicBlock block in spuBasicBlocks)
			{
				SpuInstruction inst = block.Head;
				while (inst != null)
				{
					code.Add(inst);
					inst = inst.Next;
				}
			}

			Set<VirtualRegister> spilledRegister = new Set<VirtualRegister>();


			Dictionary<VirtualRegister, SortedLinkedList<LiveInterval>> hardRegToList =
				new Dictionary<VirtualRegister, SortedLinkedList<LiveInterval>>();

			Dictionary<SortedLinkedList<LiveInterval>, VirtualRegister> ListToHardReg =
				new Dictionary<SortedLinkedList<LiveInterval>, VirtualRegister>();

			SortedLinkedList<LiveInterval> overlapingHardReg = new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());

			Dictionary<VirtualRegister, LiveInterval> currentIntervals = new Dictionary<VirtualRegister, LiveInterval>();

			foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
			{
				hardRegToList[register] = new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByStart());
				currentIntervals[register] = new LiveInterval();
				currentIntervals[register].Start = -1;
				currentIntervals[register].End = -1;
				currentIntervals[register].r = register;
			}

			foreach (VirtualRegister register in HardwareRegister.CalleeSavesRegisters)
			{
				hardRegToList[register] = new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByStart());
				currentIntervals[register] = new LiveInterval();
				currentIntervals[register].Start = -1;
				currentIntervals[register].End = -1;
				currentIntervals[register].r = register;
			}


			// Genarate hardware register "intervals"
			for (int i = 0; i < code.Count; i++)
			{
				foreach (VirtualRegister register in code[i].Use)
				{
					if (HardwareRegister.CalleeSavesRegisters.Contains(register) ||
					    HardwareRegister.CallerSavesRegisters.Contains(register))
					{
						currentIntervals[register].End = i;
					}
				}

				if (code[i].Def != null && (HardwareRegister.CalleeSavesRegisters.Contains(code[i].Def) ||
				                            HardwareRegister.CallerSavesRegisters.Contains(code[i].Def)))
				{
					hardRegToList[code[i].Def].Add(currentIntervals[code[i].Def]);
					currentIntervals[code[i].Def] = new LiveInterval();
					currentIntervals[code[i].Def].Start = i;
					currentIntervals[code[i].Def].End = i;
					currentIntervals[code[i].Def].r = code[i].Def;
				}

//				if(in)
			}

			foreach (KeyValuePair<VirtualRegister, LiveInterval> pair in currentIntervals)
				hardRegToList[pair.Key].Add(pair.Value);


			foreach (KeyValuePair<VirtualRegister, SortedLinkedList<LiveInterval>> pair in hardRegToList)
			{
				hardwareIntervals.Add(pair.Value);
				ListToHardReg.Add(pair.Value, pair.Key);
			}


			SortedLinkedList<LiveInterval> activeIntervals =
				new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());


//			Stack<CellRegister> freeRegisters = new Stack<CellRegister>();

//			foreach (CellRegister register in HardwareRegister.getCalleeSavesCellRegisters())
//			{
//				freeRegisters.Push(register);
//			}
//			foreach (CellRegister register in HardwareRegister.getCallerSavesCellRegisters())
//        	{
//        		freeRegisters.Push(register);	
//        	}

			bool isSpill = false;


			Set<VirtualRegister> hardwareRegisters = new Set<VirtualRegister>();
			hardwareRegisters.AddAll(HardwareRegister.VirtualHardwareRegisters);

// 				new SimpleRegAlloc().hardwareIntervalsToString(hardwareIntervals)

			foreach (LiveInterval interval in liveIntervals)
			{
				// ExpireOldIntervals
				while (activeIntervals.Count > 0 && activeIntervals.Head.End < interval.Start)
				{
					LiveInterval li = activeIntervals.RemoveHead();

					SortedLinkedList<LiveInterval> list;

					if (hardRegToList.TryGetValue(HardwareRegister.GetHardwareRegister((int) li.r.Register), out list))
						hardwareIntervals.Add(list);
				}

				while (hardwareIntervals.Head != null && hardwareIntervals.Head.Head != null &&
				       hardwareIntervals.Head.Head.Start <= interval.Start)
				{
					SortedLinkedList<LiveInterval> list = hardwareIntervals.RemoveHead();
					LiveInterval i = list.RemoveHead();

					while (i != null && i.End < interval.Start)
						i = list.RemoveHead();

					if (i != null)
						overlapingHardReg.Add(i);
					else
						hardwareIntervals.Add(list);
				}

				while (overlapingHardReg.Head != null && overlapingHardReg.Head.End < interval.Start)
				{
					LiveInterval i = overlapingHardReg.RemoveHead();

					SortedLinkedList<LiveInterval> list = hardRegToList[i.r];

					while (list.Head != null && list.Head.End < interval.Start)
						list.RemoveHead();

					if (list.Head != null && list.Head.Start <= interval.Start)
						overlapingHardReg.Add(list.Head);
					else
						hardwareIntervals.Add(list);
				}


//				while (hardwareIntervals.Head != null && hardwareIntervals.Head.Head != null &&
//					hardwareIntervals.Head.Head.End <= interval.Start)
//				{
//					SortedLinkedList<LiveInterval> list = hardwareIntervals.RemoveHead();
//					list.RemoveHead();
//					hardwareIntervals.Add(list);
//				}

//				foreach (SortedLinkedList<LiveInterval> list in hardwareIntervals)
//					while (list.Head != null && list.Head.End < interval.Start)
//					{
//						hardwareIntervals.Remove(list);
//						list.RemoveHead();
//						hardwareIntervals.Add(list);
//					}

				bool toBeSpilled = true;

				if (hardwareIntervals.Count != 0)
				{
					foreach (SortedLinkedList<LiveInterval> list in hardwareIntervals)
					{
						if (list.Head == null || list.Head.Start > interval.End)
						{
							interval.r.Register = ListToHardReg[list].Register;
							hardwareIntervals.Remove(list);
							toBeSpilled = false;
							break;
						}
					}
				}
				if (toBeSpilled)
				{
					// SpillAtInterval
					isSpill = true;
					LiveInterval spill = activeIntervals.Tail;
					if (spill != null && spill.End > interval.End)
					{
						interval.r.Location = spill.r.Location;
						spilledRegister.Add(spill.r);
						activeIntervals.RemoveTail();
						activeIntervals.Add(interval);
					}
					else
					{
						spilledRegister.Add(interval.r);
					}
				}
			}

			if (spilledRegister.Count > 0 && inputNewSpillOffset == null)
				throw new Exception("SimpleRegAlloc needs to spill, but no stack offset were given.");

			Dictionary<VirtualRegister, int> spillOffset = new Dictionary<VirtualRegister, int>();
			foreach (VirtualRegister register in spilledRegister)
				spillOffset.Add(register, inputNewSpillOffset());

			foreach (SpuBasicBlock block in spuBasicBlocks)
			{
				SpuInstruction inst = block.Head;
				SpuInstruction prevInst = null;

				while (inst != null)
				{
					if (inst.Def != null && spilledRegister.Contains(inst.Def))
					{
						VirtualRegister vt = HardwareRegister.GetVirtualHardwareRegister((CellRegister) 79);

						inst.Rt = vt;

						SpuInstruction stor = new SpuInstruction(SpuOpCode.stqd);

						stor.Rt = vt;
						stor.Constant = spillOffset[inst.Def];
						stor.Ra = HardwareRegister.SP;

						SpuInstruction next = inst.Next;
						inst.Next = stor;
						stor.Prev = inst;
						stor.Next = next;
						if (next != null)
							next.Prev = stor;
					}
					if (inst.Ra != null && spilledRegister.Contains(inst.Ra))
					{
						VirtualRegister vt = HardwareRegister.GetVirtualHardwareRegister((CellRegister) 78);

						inst.Ra = vt;

						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

						load.Rt = vt;
						load.Constant = spillOffset[inst.Ra];
						load.Ra = HardwareRegister.SP;

						if (prevInst != null)
						{
							prevInst.Next = load;
							load.Prev = prevInst;
						}
						else
						{
							block.Head = load;
						}

						load.Next = inst;
						inst.Prev = load;
					}
					if (inst.Rb != null && spilledRegister.Contains(inst.Rb))
					{
						VirtualRegister vt = HardwareRegister.GetVirtualHardwareRegister((CellRegister) 77);

						inst.Rb = vt;

						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

						load.Rt = vt;
						load.Constant = spillOffset[inst.Rb];
						load.Ra = HardwareRegister.SP;

						if (prevInst != null)
						{
							prevInst.Next = load;
							load.Prev = prevInst;
						}
						else
						{
							block.Head = load;
						}

						load.Next = inst;
						inst.Prev = load;
					}
					if (inst.Rc != null && spilledRegister.Contains(inst.Rc))
					{
						VirtualRegister vt = HardwareRegister.GetVirtualHardwareRegister((CellRegister) 76);

						inst.Rc = vt;

						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

						load.Rt = vt;
						load.Constant = spillOffset[inst.Rc];
						load.Ra = HardwareRegister.SP;

						if (prevInst != null)
						{
							prevInst.Next = load;
							load.Prev = prevInst;
						}
						else
						{
							block.Head = load;
						}

						load.Next = inst;
						inst.Prev = load;
					}
					if (inst.Rt != null && spilledRegister.Contains(inst.Rt))
					{
						VirtualRegister vt = HardwareRegister.GetVirtualHardwareRegister((CellRegister) 79);

						inst.Rt = vt;

						SpuInstruction load = new SpuInstruction(SpuOpCode.lqd);

						load.Rt = vt;
						load.Constant = spillOffset[inst.Rt];
						load.Ra = HardwareRegister.SP;

						if (prevInst != null)
						{
							prevInst.Next = load;
							load.Prev = prevInst;
						}
						else
						{
							block.Head = load;
						}

						load.Next = inst;
						inst.Prev = load;
					}

					prevInst = inst;
					inst = prevInst.Next;
				}
			}

			// To be safe, we insert the VirtualRegister representing the hardware registers.
			foreach (SpuBasicBlock block in spuBasicBlocks)
			{
				SpuInstruction inst = block.Head;
				while (inst != null)
				{
					if (inst.Rt != null && !hardwareRegisters.Contains(inst.Rt))
					{
						inst.Rt = HardwareRegister.GetVirtualHardwareRegister(inst.Rt.Register);
					}
					if (inst.Ra != null && !hardwareRegisters.Contains(inst.Ra))
					{
						inst.Ra = HardwareRegister.GetVirtualHardwareRegister(inst.Ra.Register);
					}
					if (inst.Rb != null && !hardwareRegisters.Contains(inst.Rb))
					{
						inst.Rb = HardwareRegister.GetVirtualHardwareRegister(inst.Rb.Register);
					}
					if (inst.Rc != null && !hardwareRegisters.Contains(inst.Rc))
					{
						inst.Rc = HardwareRegister.GetVirtualHardwareRegister(inst.Rc.Register);
					}
					inst = inst.Next;
				}
			}
			return isSpill;
		}

		private static Dictionary<VirtualRegister, LiveInterval> GenerateLiveIntervals(Set<VirtualRegister>[] liveOut)
		{
			Dictionary<VirtualRegister, LiveInterval> liveIntervals = new Dictionary<VirtualRegister, LiveInterval>();

			for (int i = 0; i < liveOut.Length; i++)
			{
				foreach (VirtualRegister register in liveOut[i])
				{
					if (register.Number == 1)
						Console.WriteLine();

					LiveInterval interval;
					if (liveIntervals.TryGetValue(register, out interval))
					{
						// Extend the end of the interval.
						interval.End = i;
					}
					else
					{
						// We haven't seen this vreg before; give it an interval.
						interval = new LiveInterval(register);
						interval.Start = i;
						interval.End = i;

//						interval.Start = i + 1;
//						interval.End = i + 1;

						interval.r = register;
						liveIntervals.Add(register, interval);
					}
				}
			}

			return liveIntervals;
		}

		private class ListComparator : IComparer<SortedLinkedList<LiveInterval>>
		{
			public int Compare(SortedLinkedList<LiveInterval> x, SortedLinkedList<LiveInterval> y)
			{
				if (y == null || y.Count == 0)
					return -1;

				if (x == null || x.Count == 0)
					return 1;

				if (x[0].Start < y[0].Start)
					return -1;
				else if (x[0].Start > y[0].Start)
					return 1;
				else
					return 0;
			}
		}

		public string hardwareIntervalsToString(SortedLinkedList<SortedLinkedList<LiveInterval>> hardwareIntervals)
		{
			StringBuilder result = new StringBuilder();

			foreach (SortedLinkedList<LiveInterval> list in hardwareIntervals)
			{
				result.AppendLine(list.ToString());
			}
			return result.ToString();
		}
	}
}

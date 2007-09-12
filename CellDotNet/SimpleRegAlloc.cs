using System;
using System.Collections.Generic;

namespace CellDotNet
{
    class SimpleRegAlloc
    {
        // TODO Registre allokatoren bør arbejde på hele metoden.
        // returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
        // regnum, er altallet af registre som register allokatoren ikke bruger.

		public static bool alloc(List<SpuBasicBlock> spuBasicBlocks, RegAllocGraphColloring.NewSpillOffsetDelegate inputNewSpillOffset, Dictionary<VirtualRegister, int> inputRegisterWeight)
        {
			Set<VirtualRegister>[] liveIn;
			Set<VirtualRegister>[] liveOut;

			IterativLivenessAnalyser.Analyse(spuBasicBlocks, out liveIn, out liveOut);
			Dictionary<VirtualRegister, LiveInterval> regToLiveDict = GenerateLiveIntervals(liveOut);
			Dictionary<LiveInterval, VirtualRegister> liveToRegDict = new Dictionary<LiveInterval, VirtualRegister>();

			List<LiveInterval> liveIntervals = new List<LiveInterval>();

			foreach (KeyValuePair<LiveInterval, VirtualRegister> pair in liveToRegDict)
			{
				regToLiveDict.Add(pair.Value, pair.Key);
				liveIntervals.Add(pair.Key);
			}

			LiveInterval.sortByStart(liveIntervals);

			List<SpuInstruction> code = new List<SpuInstruction>();

			foreach (SpuBasicBlock block in spuBasicBlocks)
        	{
        		SpuInstruction inst = block.Head;
				while(inst != null)
				{
					code.Add(inst);
					inst = inst.Next;
				}
        	}

            SortedLinkedList<LiveInterval> activeIntervals =
                new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());
			Stack<CellRegister> freeRegisters = new Stack<CellRegister>();

			foreach (CellRegister register in HardwareRegister.getCalleeSavesCellRegisters())
			{
				freeRegisters.Push(register);
			}
			foreach (CellRegister register in HardwareRegister.getCallerSavesCellRegisters())
        	{
        		freeRegisters.Push(register);	
        	}

			bool isSpill = false;


			Set<VirtualRegister> hardwareRegisters = new Set<VirtualRegister>();
			hardwareRegisters.AddAll(HardwareRegister.VirtualHardwareRegisters);

            foreach (LiveInterval interval in liveIntervals)
            {
                // ExpireOldIntervals
				while (activeIntervals.Count > 0 && activeIntervals.Head.End < interval.Start)
                {
                    LiveInterval li = activeIntervals.RemoveHead();
                    freeRegisters.Push(li.r.Register);
                }

				if (freeRegisters.Count == 0)
				{
					//TODO der skal laves plads på stakken.
					throw new Exception("Spill is not implemented.");

					/*
						// SpillAtInterval
						isSpill = true;
						LiveInterval spill = activeIntervals.Tail;
						if (spill.end > interval.end)
						{
							interval.r.Location = spill.r.Location;
							spill.r.Location = new StackLocation(); // Bør implementeres som en factory på "mothoden"
							activeIntervals.RemoveTail();
							activeIntervals.Add(interval);
						}
						else
						{
							interval.r.Location = new StackLocation(); // Bør implementeres som en factory på "mothoden"
						}
					 */
				}
				else
				{
					if (!hardwareRegisters.Contains(interval.r))
					{
						interval.r.Register = freeRegisters.Pop();
						activeIntervals.Add(interval);
					}
				}
            }

			// To be safe, we insert the VirtualRegister representing the hardware registers.
			foreach (SpuBasicBlock block in spuBasicBlocks)
			{
				SpuInstruction inst = block.Head;
				while(inst != null)
				{
					if(inst.Rt != null && !hardwareRegisters.Contains(inst.Rt))
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
			Dictionary<VirtualRegister, LiveInterval> liveIntevals = new Dictionary<VirtualRegister, LiveInterval>();

			for (int i = 0; i < liveOut.Length; i++ )
			{
				foreach (VirtualRegister register in liveOut[i])
				{
					LiveInterval interval;
					if (liveIntevals.TryGetValue(register, out interval))
					{
						interval.End = i;
					}
					else
					{
						interval = new LiveInterval();
						interval.Start = i;
						interval.End = i;
						liveIntevals.Add(register, interval);
					}
				}
			}
			return liveIntevals;
		}
    }
}

using System;
using System.Collections.Generic;

namespace CellDotNet
{
    class SimpleRegAlloc
    {
        // TODO Registre allokatoren bør arbejde på hele metoden.
        // returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
        // regnum, er altallet af registre som register allokatoren ikke bruger.

//		List<SpuBasicBlock> inputBasicBlocks, NewSpillOffsetDelegate inputNewSpillOffset
//		public bool alloc(MethodCompiler method)
		public bool alloc(List<SpuBasicBlock> spuBasicBlocks)
        {
			List<SpuInstruction> code = new List<SpuInstruction>();
			// TODO genarate codelist from method.

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

        	foreach (CellRegister register in HardwareRegister.getCallerSavesCellRegisters())
        	{
        		freeRegisters.Push(register);	
        	}
//			foreach (CellRegister register in HardwareRegister.getCalleeSavesCellRegisters())
//			{
//				freeRegisters.Push(register);
//			}

			bool isSpill = false;
            List<LiveInterval> liveIntervals = SimpleLiveAnalyzer.Analyze(code);
            LiveInterval.sortByStart(liveIntervals);

			Set<VirtualRegister> hardwareRegisters = new Set<VirtualRegister>();
			hardwareRegisters.AddAll(HardwareRegister.VirtualHardwareRegisters);

            foreach (LiveInterval interval in liveIntervals)
            {
                // ExpireOldIntervals
				while (activeIntervals.Count > 0 && activeIntervals.Head.end < interval.start)
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
    }
}

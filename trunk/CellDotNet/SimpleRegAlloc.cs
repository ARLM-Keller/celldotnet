using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    class SimpleRegAlloc
    {
        // TODO Registre allokatoren bør arbejde på hele metoden.
        // returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
        // regnum, er altallet af registre som register allokatoren ikke bruger.
        public bool alloc(List<SpuInstruction> code, int regnum)
        {
            SortedLinkedList<LiveInterval> activeIntervals =
                new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());
            Stack<StoreLocation> freeRegisters = HardwareRegister.GetCellRegistersAsStack();
            for (int i = 1; i <= regnum; i++) 
                freeRegisters.Pop();

        bool isSpill = false;
            List<LiveInterval> liveIntervals = SimpleLiveAnalyzer.Analyze(code);
            LiveInterval.sortByStart(liveIntervals);

            foreach (LiveInterval interval in liveIntervals)
            {
                // ExpireOldIntervals
				while (activeIntervals.Count > 0 && activeIntervals.Head.end < interval.start)
                {
                    LiveInterval li = activeIntervals.RemoveHead();
                    freeRegisters.Push(li.r.Location);
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
                    interval.r.Location = freeRegisters.Pop();
                    activeIntervals.Add(interval);
                }
            }
			return isSpill;
        }
    }
}

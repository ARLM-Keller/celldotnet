using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    class RegAlloc
    {
        // TODO Registre allokatoren bør arbejde på hele metoden.
        // returnere true hvis der forekommer spill(indtilvidre håndteres spill ikke!)
        public bool alloc(List<SpuInstruction> code)
        {
            SortedLinkedList<LiveInterval> activeIntervals = new SortedLinkedList<LiveInterval>(new LiveInterval.ComparByEnd());
            Stack<StorLocation> freeRegisters = HardwareRegister.getCellRegistersAsStack();
            bool isSpill = false;
            List<LiveInterval> liveIntervals = SimpleLiveAnalyzer.Analyze(code);
            LiveInterval.sortByStart(liveIntervals);

            foreach (LiveInterval interval in liveIntervals)
            {
                // ExpireOldIntervals
                while (activeIntervals.Head.end < interval.start)
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

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
            Stack<StorLocation> freeRegisters = HardwareRegister.getCellRegisteres();
            int maxRegisters = freeRegisters.Count;
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

                if (activeIntervals.Count == maxRegisters)
                {
                    // SpillAtInterval
                    isSpill = true;
                    LiveInterval spill = activeIntervals.Tail;
                    if (spill.end > interval.end)
                    {
                        interval.r.Location = spill.r.Location;
                        spill.r.Location = new StorLocation(); //TODO der skal laves plads på stakken.
                        activeIntervals.RemoveTail();
                        activeIntervals.Add(interval);
                    }
                    else
                    {
                        interval.r.Location = new StorLocation(); //TODO der skal laves plads på stakken.
                    }
                }
                else
                {
                    interval.r.Location = freeRegisters.Pop();
                    activeIntervals.Add(interval);
                }
            }
            return isSpill;

        }

        private void ExpireOldIntervals(LiveInterval i)
        {
            while (activeIntervals.Head.end < i.start)
            {
                LiveInterval li = activeIntervals.RemoveHead();
                freeRegisters.Push(li.r.Location);
            }
        }

        private void SpillAtInterval(LiveInterval li)
        {
            isSpill = true;
            LiveInterval spill = activeIntervals.Tail;
            if (spill.end > li.end)
            {
                li.r.Location = spill.r.Location;
                spill.r.Location = new StorLocation(); //TODO der skal laves plads på stakken.
                activeIntervals.RemoveTail();
                activeIntervals.Add(li);
            }
            else
            {
                li.r.Location = new StorLocation(); //TODO der skal laves plads på stakken.
            }
        }
    }
}

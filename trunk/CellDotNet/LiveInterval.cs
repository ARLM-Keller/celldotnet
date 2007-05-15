using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    public class LiveInterval
    {
        public int start, end;
        public VirtualRegister r;

        public static List<LiveInterval> sortByStart(List<LiveInterval> liveIntervals)
        {
            liveIntervals.Sort(new ComparByStart());

            return liveIntervals;
        }

        public static List<LiveInterval> sortByEnd(List<LiveInterval> liveIntervals)
        {
            liveIntervals.Sort(new ComparByEnd());

            return liveIntervals;
        }

        public class ComparByStart : IComparer<LiveInterval>
        {
            int IComparer<LiveInterval>.Compare(LiveInterval li1, LiveInterval li2)
            {
                return li1.start.CompareTo(li2.start);
            }
        }

        public class ComparByEnd : IComparer<LiveInterval>
        {
            int IComparer<LiveInterval>.Compare(LiveInterval li1, LiveInterval li2)
            {
                return li1.end.CompareTo(li2.end);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace CellDotNet
{
    public class LiveInterval
    {
		private int _start;
		private int _end;

		public int Start
    	{
    		get { return _start; }
    		set { _start = value; }
    	}

    	public int End
    	{
    		get { return _end; }
    		set { _end = value; }
    	}

        public VirtualRegister r;

		public VirtualRegister asignedRegister;

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
                return li1._start.CompareTo(li2._start);
            }
        }

		public class ComparByEnd : IComparer<LiveInterval>
        {
            int IComparer<LiveInterval>.Compare(LiveInterval li1, LiveInterval li2)
            {
                return li1._end.CompareTo(li2._end);
            }
        }

		public override string ToString()
		{
			return r + " from " + _start + " to " + _end;
		}

    }
}

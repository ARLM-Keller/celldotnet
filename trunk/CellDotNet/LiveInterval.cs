using System;
using System.Collections.Generic;

namespace CellDotNet
{
    public class LiveInterval
    {
		private int _start;
		private int _end;

    	private VirtualRegister _virtualRegister;

    	public VirtualRegister VirtualRegister
    	{
    		get { return _virtualRegister; }
    	}


//		[Obsolete()]
#warning make this obsolete
    	public LiveInterval()
    	{
    	}

    	public LiveInterval(VirtualRegister register)
    	{
    		_virtualRegister = register;
    	}

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

#warning Make this obsolete. and use Register instead.
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
            	return li1._start - li2._start;
            }

			public static readonly ComparByStart Instance = new ComparByStart();
        }

		public class ComparByEnd : IComparer<LiveInterval>
        {
            int IComparer<LiveInterval>.Compare(LiveInterval li1, LiveInterval li2)
            {
				return li1._end - li2._end;
			}

			public static readonly ComparByEnd Instance = new ComparByEnd();
		}

		public override string ToString()
		{
			return r + " from " + _start + " to " + _end;
		}

    }
}

using System;
using System.Collections.Generic;

namespace CellDotNet
{
    public class LiveInterval
    {
		private int _start;
		private int _end;

    	private VirtualRegister _virtualRegister;

		/// <summary>
		/// The virtual register which is live in the interval.
		/// It can be a pure virtual register or a hardware register, depending on precoloring and
		/// the allocator.
		/// </summary>
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

		/// <summary>
		/// This is only to be used by the graph coloring allocator since it's read-write,
		/// and the linear allocator works by changing the <see cref="VirtualRegister"/>'s <see cref="CellRegister"/> property.
		/// </summary>
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
            public int Compare(LiveInterval li1, LiveInterval li2)
            {
            	return li1._start - li2._start;
            }

			public static readonly ComparByStart Instance = new ComparByStart();
        }

		public class ComparByEnd : IComparer<LiveInterval>
        {
            public int Compare(LiveInterval li1, LiveInterval li2)
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

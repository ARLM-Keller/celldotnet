// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CellDotNet.Spe
{
	class LiveInterval
	{
		private int _start;
		private int _end;

		private readonly VirtualRegister _virtualRegister;

		/// <summary>
		/// The virtual register which is live in the interval.
		/// It can be a pure virtual register or a hardware register, depending on precoloring and
		/// the allocator.
		/// </summary>
		public VirtualRegister VirtualRegister
		{
			get { return _virtualRegister; }
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

		public static List<LiveInterval> SortByStart(List<LiveInterval> liveIntervals)
		{
			liveIntervals.Sort(new CompareByStart());

			return liveIntervals;
		}

		public static List<LiveInterval> SortByEnd(List<LiveInterval> liveIntervals)
		{
			liveIntervals.Sort(new CompareByEnd());

			return liveIntervals;
		}

		public class CompareByStart : IComparer<LiveInterval>
		{
			public int Compare(LiveInterval li1, LiveInterval li2)
			{
				return li1._start - li2._start;
			}

			public static readonly CompareByStart Instance = new CompareByStart();
		}

		public class CompareByEnd : IComparer<LiveInterval>
		{
			public int Compare(LiveInterval li1, LiveInterval li2)
			{
				return li1._end - li2._end;
			}

			public static readonly CompareByEnd Instance = new CompareByEnd();
		}

		public override string ToString()
		{
			return VirtualRegister + " from " + _start + " to " + _end;
		}
	}
}

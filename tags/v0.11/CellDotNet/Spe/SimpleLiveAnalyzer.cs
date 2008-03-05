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
	internal class SimpleLiveAnalyzer
	{
		public static List<LiveInterval> Analyze(List<SpuInstruction> code)
		{
			Dictionary<VirtualRegister, LiveInterval> liveIntervals = new Dictionary<VirtualRegister, LiveInterval>();

			SortedLinkedList<LiveInterval> intervallist = new SortedLinkedList<LiveInterval>(new LiveInterval.CompareByEnd());

			for (int i = 0; i < code.Count; i++)
			{
				VirtualRegister def = code[i].Rt;
				ICollection<VirtualRegister> uses = code[i].Use;

				LiveInterval li;
				//li = liveIntervals[Def];
				if (def != null)
				{
					if (!liveIntervals.TryGetValue(def, out li))
					{
						li = new LiveInterval(def);
						li.Start = i;
						li.End = i;
						liveIntervals.Add(def, li);
						intervallist.Add(li);
					}
					else
						li.End = i;
				}
				foreach (VirtualRegister r in uses)
				{
					//li = liveIntervals[r];
					if (!liveIntervals.TryGetValue(r, out li))
					{
						li = new LiveInterval(r);
						li.Start = i;
						li.End = i;
						liveIntervals.Add(r, li);
						intervallist.Add(li);
					}
					else
						li.End = i;
				}

				switch (code[i].OpCode.Name)
				{
					case "br":
					case "brsl":
					case "brnz":
					case "brz":
					case "brhnz":
					case "brhz":
						Int16 desti = (Int16) code[i].Constant;
						if (desti >= 0)
							break;
						int dest = i + desti;
						SortedLinkedList<LiveInterval>.Node<LiveInterval> n = intervallist.getNodeAt(intervallist.Count - 1);
						while (n.Data.End >= dest)
						{
							n.Data.End = i;
							if (n.Data.Start > dest)
								n.Data.Start =
									dest;
						}
						break;
					case "bra":
					case "brasl":
					case "bi":
					case "iret":
					case "bisled":
					case "bisl":
					case "biz":
					case "binz":
					case "bihz":
					case "bihnz":
						throw new RegisterAllocationException(
							string.Format("Unable to handle \"{0}\" instruction in register alloction.", code[i].OpCode.Name));
					default:
						break;
				}
			}
			return new List<LiveInterval>(liveIntervals.Values);
		}
	}
}
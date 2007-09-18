using System;
using System.Collections.Generic;

namespace CellDotNet
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
				ICollection<VirtualRegister> uses = code[i].Sources;

				LiveInterval li;
				//li = liveIntervals[def];
				if (def != null)
				{
					if (!liveIntervals.TryGetValue(def, out li))
					{
						li = new LiveInterval();
						li.Start = i;
						li.End = i;
						li.r = def;
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
						li = new LiveInterval();
						li.Start = i;
						li.End = i;
						li.r = r;
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
						throw new Exception(
							string.Format("Unabel to handel \"{0}\" instruction in register alloction.", code[i].OpCode.Name));
					default:
						break;
				}
			}
			return new List<LiveInterval>(liveIntervals.Values);
		}
	}
}
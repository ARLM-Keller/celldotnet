using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    class SimpleLiveAnalyzer
    {
        public static List<LiveInterval> Analyze(List<SpuInstruction> code)
        {
            Dictionary<VirtualRegister, LiveInterval> liveIntervals = new Dictionary<VirtualRegister, LiveInterval>();

            for (int i = 0; i < code.Count; i++)
            {
                VirtualRegister def = code[i].Rt;
                ICollection<VirtualRegister> uses = code[i].Sources;

                LiveInterval li = null;

                li = liveIntervals[def];
                if (li == null)
                {
                    li = new LiveInterval();
                    li.start = i;
                    li.end = i;
                    li.r = def;
                    liveIntervals.Add(def, li);
                }
                else
                {
                    li.end = i;
                }

                foreach (VirtualRegister r in uses)
                {
                    li = liveIntervals[r];
                    if (li == null)
                    {
                        li = new LiveInterval();
                        li.start = i;
                        li.end = i;
                        li.r = r;
                        liveIntervals.Add(r, li);
                    }
                    else
                    {
                        li.end = i;
                    }
                }
            }
            return new List<LiveInterval>(liveIntervals.Values);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SimpleRegAllocTest : UnitTest
	{
		[Test]
		public void TestLiveIntervals()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();

			// Write some instructions with known live intervals.
			w.BeginNewBasicBlock();
			VirtualRegister unused0 = w.WriteLoadI4(4);
			VirtualRegister unused1 = w.WriteLoadI4(4);

			VirtualRegister live2_3 = w.WriteLoadI4(5);
			w.WriteAi(live2_3, 6);

			VirtualRegister arg0_4 = new VirtualRegister(10);
			w.WriteAi(arg0_4, 7);


			StringWriter sw = new StringWriter();
			Disassembler.DisassembleInstructions(w.GetAsList(), 0, sw);
			Console.WriteLine(sw.GetStringBuilder());

			List<LiveInterval> intlist = SimpleRegAlloc.CreateSortedLiveIntervals(w.BasicBlocks);
			Dictionary <VirtualRegister, LiveInterval> intdict = new Dictionary<VirtualRegister, LiveInterval>();
			foreach (LiveInterval i in intlist)
				intdict[i.VirtualRegister] = i;

			AreEqual(0, intdict[unused0].Start, "Bad start for unused0 " + intdict[unused0].VirtualRegister);
			// unused0 and unused1 could really end 0 and 1, but it would make interval construction 
			// more complicated.
			AreEqual(1, intdict[unused0].End, "Bad end for unused0 " + intdict[unused0].VirtualRegister);

			AreEqual(1, intdict[unused1].Start, "Bad start for unused1 " + intdict[unused1].VirtualRegister);
			AreEqual(2, intdict[unused1].End, "Bad end for unused1 " + intdict[unused1].VirtualRegister);

			AreEqual(2, intdict[live2_3].Start, "Bad start for live2_3 " + intdict[live2_3].VirtualRegister);
			AreEqual(3, intdict[live2_3].End, "Bad end for live2_3 " + intdict[live2_3].VirtualRegister);

			AreEqual(0, intdict[arg0_4].Start, "Bad start for arg0_4 " + intdict[arg0_4].VirtualRegister);
			AreEqual(4, intdict[arg0_4].End, "Bad end for arg0_4 " + intdict[arg0_4].VirtualRegister);
		}
	}
}
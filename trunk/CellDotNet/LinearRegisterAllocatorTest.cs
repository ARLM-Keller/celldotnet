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
using System.IO;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class LinearRegisterAllocatorTest : UnitTest
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


//			StringWriter sw = new StringWriter();
//			Disassembler.DisassembleInstructions(w.GetAsList(), 0, sw);
//			Console.WriteLine(sw.GetStringBuilder());

			List<LiveInterval> intlist = LinearRegisterAllocator.CreateSortedLiveIntervals(w.BasicBlocks);
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
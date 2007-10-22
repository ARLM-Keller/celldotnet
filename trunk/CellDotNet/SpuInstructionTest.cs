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
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuInstructionTest : UnitTest
	{
		[Test]
		public void TestRI16()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.brsl);
			inst.Constant = 50;
			inst.Rt = HardwareRegister.GetHardwareRegister(3);
			AreEqual("001100110" + "0000000000110010" + "0000011", Convert.ToString(inst.Emit(), 2).PadLeft(32, '0'));
		}

		[Test]
		public void TestRI10()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqd);
			inst.Constant = 0x2AA;
			inst.Rt = HardwareRegister.GetHardwareRegister(80);
			inst.Ra = HardwareRegister.GetHardwareRegister(81);
			AreEqual("00100100" + "1010101010" + "1010001" + "1010000", Convert.ToString(inst.Emit(), 2).PadLeft(32, '0'));
		}

		[Test]
		public void TestRI10_2()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqd);
			inst.Constant = 2;
			inst.Rt = HardwareRegister.GetHardwareRegister(80);
			inst.Ra = HardwareRegister.SP;
			int bin = inst.Emit();
			Console.WriteLine(bin.ToString("x8"));
			AreEqual("00100100" + "0000000010" + "0000001" + "1010000", Convert.ToString(bin, 2).PadLeft(32, '0'));
		}

		[Test]
		public void TestIohl()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			w.WriteIohl(4);
			SpuInstruction iohl = w.LastInstruction;

			AreEqual(1, iohl.Use.Count);
			IsNotNull(iohl.Def);
		}

		[Test]
		public void TestIl()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			w.WriteIl(4);
			SpuInstruction il = w.LastInstruction;

			AreEqual(0, il.Use.Count);
			IsNotNull(il.Def);
		}
	}
}

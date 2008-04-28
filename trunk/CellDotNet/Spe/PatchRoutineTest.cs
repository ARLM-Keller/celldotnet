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

#if UNITTEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Spe
{
	[TestFixture]
	public class PatchRoutineTest : UnitTest
	{
		[Test]
		public void Test1()
		{
			// Generate random code.
			var rawcode = new int[20];
			var rand = new Random(33333);
			for (int i = 0; i < rawcode.Length; i++)
				rawcode[i] = rand.Next();

			// Make some changes.
			PatchRoutine r = new PatchRoutine(rawcode);
			ObjectWithAddress obj = new DataObject(16);
			r.Seek(0x10);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);
			r.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
			r.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
			r.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);

			r.Seek(0x30);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);

			r.Offset = 0x100;
			obj.Offset = 0x200;
			r.PerformAddressPatching();

			// Check that the changes are what we expect them to be.
			int[] code = r.Emit();
			int[] diffIndices = Enumerable.Range(0, 20).Where(i => rawcode[i] != code[i]).ToArray();
			AreEqual(new[] { 4, 5, 6, 7, 8, 12 }, diffIndices);

			// Get the same code in a version we're pretty certain is correct.
			{
				ManualRoutine res = new ManualRoutine(true) { Offset = 0x110 };
				res.Writer.BeginNewBasicBlock();
				res.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);
				res.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
				res.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
				res.Writer.WriteAi(HardwareRegister.LR, HardwareRegister.LR, 33);
				res.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);
				res.PerformAddressPatching();

				AreEqual(res.Emit(), SubArray(code, 4, 5));
			}
			AreEqual(GetInstructionWord(obj, 0x120, SpuOpCode.brsl), code[8]);
		}

		private int[] SubArray(int[] array, int startindex, int count)
		{
			int[] newarr = new int[count];
			Buffer.BlockCopy(array, startindex * 4, newarr, 0, count * 4);
			return newarr;
		}

		private static int GetInstructionWord(ObjectWithAddress target, int instructionOffset, SpuOpCode opcode)
		{
			ManualRoutine res = new ManualRoutine(true) { Offset = instructionOffset };
			res.Writer.BeginNewBasicBlock();
			res.Writer.WriteRelativeAddressInstruction(opcode, HardwareRegister.LR, target);
			res.PerformAddressPatching();
			int[] code = res.Emit();
			AreEqual(1, code.Length);

			return code[0];
		}

		[Test]
		public void Test2()
		{
			// Generate random code.
			var rawcode = new int[20];
			var rand = new Random(33333);
			for (int i = 0; i < rawcode.Length; i++)
				rawcode[i] = rand.Next();

			// Make some changes.
			PatchRoutine r = new PatchRoutine(rawcode);
			ObjectWithAddress obj = new DataObject(16);
			r.Seek(0x10);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);
			r.Seek(0x20);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);
			r.Seek(0x30);
			r.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, obj);

			r.Offset = 0x100;
			obj.Offset = 0x200;
			r.PerformAddressPatching();
			r.Emit();
		}

		[Test]
		public void TestNoCode()
		{
			PatchRoutine r = new PatchRoutine(new int[0]);
			r.PerformAddressPatching();
			int[] code = r.Emit();
			AreEqual(new int[0], code);
		}

		[Test]
		public void TestNoModifications()
		{
			int[] rawcode = new int[] {2059824};
			PatchRoutine r = new PatchRoutine(rawcode);
			r.PerformAddressPatching();
			int[] code = r.Emit();
			AreEqual(rawcode, code);
		}
	}
}

#endif

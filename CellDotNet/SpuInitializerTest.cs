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
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuInitializerTest : UnitTest
	{
		[Test]
		public void TestInitialization()
		{
			const int magicnum = 0x3654ff;

			SpecialSpeObjects specialSpeObjects = new SpecialSpeObjects();
			specialSpeObjects.SetMemorySettings(256*1024-0x20,8*1024-0x20,128*1024,118*1024);

			// The code to run just returns.
			ManualRoutine routine = new ManualRoutine(true);
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteLoadI4(HardwareRegister.GetHardwareRegister(3), magicnum);
			routine.Writer.WriteBi(HardwareRegister.LR);
			routine.WriteEpilog();
			routine.Offset = 512;

			RegisterSizedObject returnLocation = new RegisterSizedObject();
			returnLocation.Offset = 1024;

			int[] code = new int[1000];
			{
				// Initialization.
				SpuInitializer initializer =
					new SpuInitializer(routine, returnLocation, null, 0, specialSpeObjects.StackPointerObject,
					                   specialSpeObjects.NextAllocationStartObject, 
									   specialSpeObjects.AllocatableByteCountObject);
				specialSpeObjects.StackPointerObject.Offset = 768;
				specialSpeObjects.NextAllocationStartObject.Offset = 768 + 16;
				specialSpeObjects.AllocatableByteCountObject.Offset = 768 + 32;

				initializer.Offset = 0;
				initializer.PerformAddressPatching();
				int[] initCode = initializer.Emit();
				Buffer.BlockCopy(initCode, 0, code, initializer.Offset, initCode.Length * 4);
			}
			{
				routine.PerformAddressPatching();
				List<SpuInstruction> list = routine.Writer.GetAsList();
				int[] routineCode = SpuInstruction.Emit(list);
				Buffer.BlockCopy(routineCode, 0, code, routine.Offset, routineCode.Length * 4);
			}

			if (!SpeContext.HasSpeHardware)
				return;

			// Run
			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);

				ctx.Run();

				int retval1 = ctx.DmaGetValue<int>((LocalStorageAddress) returnLocation.Offset);

				int retval2 = ctx.DmaGetValue<int>((LocalStorageAddress)returnLocation.Offset);
				AreEqual(magicnum, retval1);
				AreEqual(magicnum, retval2);

			}
		}

		private delegate int IntDelegateTripleArg(int a, int b, int c);

		[Test]
		public void TestArguments_RunProgram()
		{
			IntDelegateTripleArg del = delegate(int a, int b, int c) { return a + b + c; };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.RunProgram(cc, new ValueType[] { 1, 2, 3 });
				int returnValue = ctx.DmaGetValue<int>(cc.ReturnValueAddress);
				AreEqual(6, returnValue, "Function call returned a wrong value.");
			}
		}
	}
}



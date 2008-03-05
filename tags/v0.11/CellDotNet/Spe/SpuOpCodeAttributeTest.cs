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
using NUnit.Framework;


namespace CellDotNet.Spe
{
	[TestFixture]
	public class SpuOpCodeAttributeTest : UnitTest
	{
		[Test]
		public void TestAddWordSubstitution()
		{
			Converter<int, int> del = delegate(int input) { return Add(input, 10); };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, 5);
				AreEqual(15, (int) rv);
			}
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static int Add(
			[SpuInstructionPart(SpuInstructionPart.Ra)]int x, 
			[SpuInstructionPart(SpuInstructionPart.Rb)]int y)
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		// *********************************************************************
		// TestAddWordSubstitution_WrongDecoration_DoubleRa
		// *********************************************************************

		[Test, ExpectedException(typeof(InvalidInstructionParametersException))]
		public void TestAddWordSubstitution_WrongDecoration_DoubleRa()
		{
			Converter<int, int> del = delegate(int input) { return Add_WithDoubleRa(input, 10); };
			SpeDelegateRunner.CreateSpeDelegate(del);
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static int Add_WithDoubleRa(
			[SpuInstructionPart(SpuInstructionPart.Ra)]int x,
			[SpuInstructionPart(SpuInstructionPart.Ra)]int y) // Ra is applied twice.
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		// *********************************************************************
		// TestAddWordSubstitution_WrongDecoration_MissingDecoration
		// *********************************************************************

		[Test, ExpectedException(typeof(InvalidInstructionParametersException))]
		public void TestAddWordSubstitution_WrongDecoration_MissingDecoration()
		{
			Converter<int, int> del = delegate(int input) { return Add_MissingDecorationRa(input, 10); };
			SpeDelegateRunner.CreateSpeDelegate(del);
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static int Add_MissingDecorationRa(
			[SpuInstructionPart(SpuInstructionPart.Ra)]int x,
			int y) // Missing decoration.
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		// *********************************************************************
		// TestAddWordSubstitution_WrongDecoration_MissingPart
		// *********************************************************************

		[Test, ExpectedException(typeof(InvalidInstructionParametersException))]
		public void TestAddWordSubstitution_WrongDecoration_MissingPart()
		{
			Action<int> del = delegate(int input) { Add_MissingPartRa(input, 10); };
			SpeDelegateRunner.CreateSpeDelegate(del);
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		private static void Add_MissingPartRa( // rt return part is not present.
			[SpuInstructionPart(SpuInstructionPart.Ra)] int x,
			[SpuInstructionPart(SpuInstructionPart.Rb)] int y) // Missing part.
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}



		[SpuOpCode(SpuOpCodeEnum.Ai)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static int AddI(
			[SpuInstructionPart(SpuInstructionPart.Ra)]int x,
			[SpuInstructionPart(SpuInstructionPart.Immediate)]int y)
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		[Test]
		public void TestAddWordImmediatedSubstitution()
		{
			Converter<int, int> del = delegate(int input) { return AddI(input, 10); };

//			MethodCompiler mc = new MethodCompiler(del.Method);
////			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
////			Disassembler.DisassembleUnconditionalToConsole(mc);
//			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);
////			new LinearRegisterAllocator().Allocate();
//			return;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.RunProgram(cc, new object[] {12});

				int returnValue = sc.DmaGetValue<int>(cc.ReturnValueAddress);

				AreEqual(22, returnValue, "");
			}
		}

		[Test, ExpectedException(typeof(InvalidInstructionParametersException))]
		public void TestAddWordImmediatedSubstitution_NotConst()
		{
			Converter<int, int> del = delegate(int input) { return AddI(input, input); };

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.RunProgram(cc, new object[] { 12 });
			}
		}
	}
}
#endif
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuOpCodeAttributeTest : UnitTest
	{
		[Test]
		public void TestAddWordSubstitution()
		{
			Converter<int, int> del = delegate(int input) { return Add(input, 10); };

			Converter<int, int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);
			SpeDelegateRunner runner = (SpeDelegateRunner) del2.Target;
			AreEqual(1, runner.CompileContext.Methods.Count);

			Disassembler.DisassembleToConsole(runner.CompileContext);


			if (!SpeContext.HasSpeHardware)
				return;

			int rv = del2(5);
			AreEqual(15, rv);
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
		// TestAddWordSubstitution_WrongDecoration_MissingPart
		// *********************************************************************

		[Test, ExpectedException(typeof(InvalidInstructionParametersException))]
		public void TestAddWordSubstitution_WrongDecoration_MissingPart()
		{
			Converter<int, int> del = delegate(int input) { return Add_WithMissingPartRa(input, 10); };
			SpeDelegateRunner.CreateSpeDelegate(del);
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static int Add_WithMissingPartRa(
			[SpuInstructionPart(SpuInstructionPart.Ra)]int x,
			int y) // Missing part.
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}
	}
}

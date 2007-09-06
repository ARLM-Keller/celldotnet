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
	}
}

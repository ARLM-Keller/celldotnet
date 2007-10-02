using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	/// <summary>
	/// For simple programs. Test focused on arrays and object should go into <see cref="ObjectModelTest"/>.
	/// </summary>
	[TestFixture]
	public class SimpleProgramsTest : UnitTest
	{
		private delegate int IntReturnDelegate();

		private delegate float FloatReturnDelegate();

		[Test]
		public void TestDump()
		{
			Converter<int, int> del = delegate(int input) { return input + 0xf0f0; };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			cc.WriteAssemblyToFile("dumpx.s", 0xaeae);
//			cc.GetEmittedCode(0xaeae);
		}

		[Test]
		public void TestLoop_SumInt()
		{
			IntReturnDelegate del =
				delegate
					{
						const int count = 5;
						int sum = 0;
						for (int i = 0; i < count; i++)
							sum += i*i;
						return sum;
					};

			int correctVal = del();

//			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			new TreeDrawer().DrawMethods(cc);

			Disassembler.DisassembleToConsole(cc);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc);
				AreEqual(correctVal, (int)rv);
			}
		}

		[Test]
		public void TestLoop_SumFloat()
		{
			FloatReturnDelegate del =
				delegate
					{
						const int count = 5;
						float sum = 0;
						for (int i = 0; i < count; i++)
							sum += (float) i*(float) i;
						return sum;
					};

			float correctVal = del();

			FloatReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, del2());
		}

		[Test]
		public void TestLoop_SumFloat2()
		{
			FloatReturnDelegate del =
				delegate
					{
						const int count = 5;
						float sum = 0;
						int[] arr = new int[count];

						for (int i = 0; i < count; i++)
							arr[i] = i;
						for (int i = 0; i < count; i++)
							sum += arr[i];

						return sum;
					};

			float correctVal = del();
			FloatReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);
			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, del2());
		}

//		[Test]
//		public void TestReturnConditional()
//		{
//			Converter<int, int> del = 
//				delegate(int input)
//					{
//						// Tests that 
//						if (input == 1)
//							return 10;
//						else
//							return 20;
//					};
//			int arg = 1;
//			CompileContext cc = new CompileContext(del.Method);
//
//			AreEqual(del(arg), (int) SpeContext.UnitTestRunProgram(cc, arg));
//		}

		#region Method call

		[Test]
		public void TestMethodCall()
		{
			Converter<int, int> del = MethodCallMethod;

			const int arg = 15;
			int correctval = del(arg);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

//			cc.WriteAssemblyToFile("TestMethodCall.s", arg);
			Disassembler.DisassembleToConsole(cc);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (int) rv);
			}
		}

		private static int ReturnInt(int i)
		{
			return i;
		}

		private static int MethodCallMethod(int level)
		{
			return 3 + ReturnInt(10);
		}

		#endregion

		// **************************************************
		// TestRecursiveSummation_Int
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Int()
		{
			Converter<int, int> del = RecursiveSummation_Int;

			const int arg = 15;
			int correctval = del(arg);
			AreNotEqual(0, correctval, "Zero isn't good.");

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);
			Disassembler.DisassembleUnconditionalToConsole((SpuDynamicRoutine) cc.EntryPoint);

			cc.PerformProcessing(CompileContextState.S8Complete);

			Disassembler.DisassembleToConsole(cc);

//			cc.WriteAssemblyToFile(Utilities.GetUnitTestName() + "_asm.s", arg);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (int) rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private static int RecursiveSummation_Int(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 3) == 1)
				return level + RecursiveSummation_Int(level - 1);
			else
				return RecursiveSummation_Int(level - 1);
		}

		// **************************************************
		// TestRecursiveSummation_Float
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Float()
		{
			Converter<int, float> del = RecursiveSummation_Float;

			const int arg = 15;
			float correctval = del(arg);
			AreNotEqual(0f, correctval, "Zero isn't good.");

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);
			Disassembler.DisassembleUnconditionalToConsole((SpuDynamicRoutine) cc.EntryPoint);

			cc.PerformProcessing(CompileContextState.S8Complete);


			Disassembler.DisassembleToConsole(cc);


			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (float) rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private static float RecursiveSummation_Float(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 3) == 1)
				return level + RecursiveSummation_Float(level - 1);
			else
				return RecursiveSummation_Float(level - 1);
		}

		[Test]
		public void TestConditionalExpresion()
		{
			Converter<int, int> fun =
				delegate(int input)
					{
						return input + (input == 17 ? 0 : input);
					};

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			object result1 = SpeContext.UnitTestRunProgram(cc, 14);
			object result2 = SpeContext.UnitTestRunProgram(cc, 17);

			AreEqual(28, (int)result1);
			AreEqual(17, (int)result2);
		}

		[Test]
		public void TestDivisionSigned()
		{
			Converter<int, int> fun =
				delegate(int input)
				{
					return input / 7;
				};

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			object result1 = SpeContext.UnitTestRunProgram(cc, 14);
			object result2 = SpeContext.UnitTestRunProgram(cc, 17);

			AreEqual(2, (int)result1);
			AreEqual(2, (int)result2);
		}

		[Test]
		public void TestImplementationDivisionUnsigned()
		{
			Assert.AreEqual(SpuMath.Div_Un(14, 7), ((uint)14)/((uint)7), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(33, 7), ((uint)33) / ((uint)7), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(12345, 54), ((uint)12345) / ((uint)54), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(987536, 664), ((uint)987536) / ((uint)664), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(9675, 745), ((uint)9675) / ((uint)745), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(123454, 3), ((uint)123454) / ((uint)3), "SpuMath.Div_Un failed.");
			Assert.AreEqual(SpuMath.Div_Un(16524, 23), ((uint)16524) / ((uint)23), "SpuMath.Div_Un failed.");
		}

		[Test]
		public void TestImplementationDivisionSigned()
		{
			Assert.AreEqual(SpuMath.Div(14, 7), 14 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(33, 7), 33 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(12345, 54), 12345 / 54, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(987536, 664), 987536 / 664, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(9675, 745), 9675 / 745, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(123454, 3), 123454 / 3, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(16524, 23), 16524 / 23, "SpuMath.Div failed.");

			Assert.AreEqual(SpuMath.Div(-14, 7), -14 / 7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(33, -7), 33 / -7, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-12345, -54), -12345 / -54, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-987536, -664), -987536 / -664, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(9675, -745), 9675 / -745, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(123454, -3), 123454 / -3, "SpuMath.Div failed.");
			Assert.AreEqual(SpuMath.Div(-16524, 23), -16524 / 23, "SpuMath.Div failed.");
		}
	}
}
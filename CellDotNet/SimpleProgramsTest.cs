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

			IntReturnDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, del2());
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
						sum += (float) i * (float) i;
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

		// **************************************************
		// TestRecursiveSummation_Int
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Int()
		{
			Converter<int, int> del = RecursiveSummation_Int;

			const int arg = 15;

			int correctval = del(arg);

//			Console.WriteLine("TestRecursiveSummation_Int: {0}", correctval);

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);

//			new TreeDrawer().DrawMethods(cc);

//			Disassembler.DisassembleUnconditional(cc, Console.Out);

			cc.PerformProcessing(CompileContextState.S8Complete);

//			Disassembler.DisassembleToConsole(cc);

			cc.WriteAssemblyToFile(Utilities.GetUnitTestName() + "_asm.s", arg);
			
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (int)rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		static int RecursiveSummation_Int(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 1) == 1)
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

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, arg);
				AreEqual(correctval, (float)rv);
			}
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		static float RecursiveSummation_Float(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 2) == 1)
				return level + RecursiveSummation_Int(level - 1);
			else
				return RecursiveSummation_Int(level - 1);
		}
	}
}

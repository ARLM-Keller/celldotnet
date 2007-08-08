using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MethodCompilerTest : UnitTest
	{
		private delegate void BasicTestDelegate();

		public void TestBranchCodeGenerationBasic()
		{
			BasicTestDelegate del = delegate
				{
					int i = 0;

					for (int j = 0; j < 10; j++)
					{
						if (i < 4)
							i = 4;
						else
							i = 10;
					}
				};


			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S7AddressPatchingDone);


			Console.WriteLine("Disassembly - prolog:");
			Console.Write(mc.GetPrologWriter().Disassemble());
			Console.WriteLine();
			Console.WriteLine("Disassembly - body:");
			Console.Write(mc.GetBodyWriter().Disassemble());
			Console.WriteLine();
			Console.WriteLine("Disassembly - epilog:");
			Console.Write(mc.GetEpilogWriter().Disassemble());

			mc.GetPrologWriter().AssertNoPseudoInstructions();
			mc.GetBodyWriter().AssertNoPseudoInstructions();
			mc.GetEpilogWriter().AssertNoPseudoInstructions();
		}

		#region Frame tests.

		private delegate void FiveIntegerArgumentDelegate(int i1, int i2, int i3, int i4, int i5);


		/// <summary>
		/// Usesd solely for making variables/arguments escape.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="arg"></param>
		static void DummyByrefFunction<T>(ref T arg)
		{
			// Nothing.
		}

		[Test]
		public void TestFrameArgumentEscapeDetection()
		{
			FiveIntegerArgumentDelegate del = delegate(int i1, int i2, int i3, int i4, int i5)
				{
					int li1 = Math.Max(234, i3);
					int li2 = Math.Max(li1, i4);

					// Arguments i1, i2 and i5 should escape, and so should li2.
					DummyByrefFunction(ref i1);
					DummyByrefFunction(ref i2);
					DummyByrefFunction(ref i5);
					DummyByrefFunction(ref li2);
				};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S3InstructionSelectionPreparationsDone);

			// Find names of escaping locals and variables.
			List<string> paramnamelist = new List<string>();
			foreach (MethodParameter p in mc.Parameters)
			{
				if (p.Escapes.Value)
					paramnamelist.Add(p.Name);
			}
			if (!Algorithms.AreEqualSets(paramnamelist, new string[] {"i1", "i2", "i5"}, StringComparer.Ordinal))
				Assert.Fail("Didn't correctly determine escaping parameters.");

			List<int> varindices = new List<int>();
			foreach (MethodVariable v in mc.Variables)
			{
				if (v.Escapes.Value)
					varindices.Add(v.Index);
			}
			if (varindices.Count != 1 || varindices[0] != 1)
				Assert.Fail("Didn't correctly determine escaping varaible.");
		}

		#endregion

		private delegate T Getter<T>();

		[Test]
		public void TestReturnValue()
		{
			Getter<int> g = delegate { return 500; };

			CompareExecution(g);
		}

		

		private void CompareExecution<T>(Getter<T> getter) where T : IComparable<T>
		{
			CompileContext cc = new CompileContext(getter.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			int[] code = cc.GetEmittedCode();
			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();
			}

			// TODO: Run both delegates and compare the return value.
			throw new NotImplementedException();

			T t1 = getter();
			T t2 = default(T);

			AreEqual(t1, t2, "SPU delegate execution returned a different value.");
		}

		[Test]
		public void Test()
		{
#if !DEBUG 
			Assert.Fail("No debug mode.");
#endif
		}
	}
}

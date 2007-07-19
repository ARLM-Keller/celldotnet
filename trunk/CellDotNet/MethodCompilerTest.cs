using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MethodCompilerTest : UnitTest
	{
		private delegate void BasicTestDelegate();

		[Test]
		public void TestBuildTree()
		{
			Converter<int, long> del =
				delegate(int i)
				{
					int j;

					char c1;
					char c2;

					j = 8 + (i * 5);
					c1 = (char)j;
					c2 = (char)(j + 1);
					j += c1 + c2;
					//						DateTime[] arr= new DateTime[0];
					if (i > 5)
						j++;
					while (j < 0)
					{
						j--;
					}
					//						int[] arr = new int[4];
					//						arr[1] = 9;

					return j * 2;
				};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(ci, method);
		}


		[Test]
		public void TestParsePopOperandIsNull()
		{
			BasicTestDelegate del =
				delegate
					{
						// will have a pop instruktion before the ret.
						Math.Max(1, 2);
					};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			TreeDrawer td = new TreeDrawer();
			td.DrawMethod(mc, del.Method);

			IsTrue(mc.Blocks.Count == 1);
			TreeInstruction popInst =
				mc.Blocks[0].Roots.Find(delegate(TreeInstruction obj) { return obj.Opcode == IROpCodes.Pop; });
			AreEqual(IRCode.Pop, popInst.Opcode.IRCode);

			// Pop should not have an operand.
			IsNull(popInst.Operand);
			AreEqual(StackTypeDescription.None, popInst.StackType);

			// The max call.
			AreEqual(StackTypeDescription.Int32, popInst.Left.StackType);
		}

		[Test]
		public void TestParseFunctionCall()
		{
			BasicTestDelegate del = delegate
										{
											int rem;
											Math.DivRem(9, 13, out rem);
											Math.Max(Math.Min(3, 1), 5L);
										};
			MethodCompiler ci = new MethodCompiler(del.Method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, del.Method);
		}

		[Test]
		public void TestParseObjectInstantiationAndInstanceMethodCall()
		{
			BasicTestDelegate del = delegate
										{
											ArrayList list = new ArrayList(34);
											list.Clear();
										};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
		}

		[Test, Ignore("Enable when arrays are supported.")]
		public void TestParseArrayInstantiation()
		{
			BasicTestDelegate del = delegate
										{
											int[] arr = new int[5];
											int j = arr.Length;
										};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci, method);
		}

		[Test, Description("Test non-trivial branching.")]
		public void TestParseBranches1()
		{
			BasicTestDelegate del = delegate
			                        	{
			                        		int i = 34;

										RestartLoop:
											for (int j = 1; j < i; j++)
											{
												if (Math.Max(j, i) > 4)
													goto RestartLoop;

												if (j % 10 == 0)
													goto NextIteration;

												i++;
												if (i == 12)
													continue;

											NextIteration: {
												throw new Exception(); }
											}
			                        	};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(mc, mc.MethodBase);
		}

		public void TestParseBranchBasic()
		{
			BasicTestDelegate del = delegate
				{
					int i = Math.Max(4, 0);

					for (int j = 0; j < 10; j++)
					{
						// It's important to have these calls to test branches to function argument
						// loading instructions.
						if (i < 4)
							Math.Log(i);
						else
							Math.Log(j);
					}
				};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
		}


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
			mc.PerformProcessing(MethodCompileState.S7BranchesFixed);


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

		[Test]
		public void Test()
		{
#if !DEBUG 
			Assert.Fail("No debug mode.");
#endif
		}
	}
}

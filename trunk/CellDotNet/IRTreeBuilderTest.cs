using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class IRTreeBuilderTest : UnitTest
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

			new TreeDrawer().DrawMethod(ci);
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
			td.DrawMethod(mc);

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
			new TreeDrawer().DrawMethod(ci);
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
			new TreeDrawer().DrawMethod(ci);
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
			new TreeDrawer().DrawMethod(ci);
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

											NextIteration:
												{
													throw new Exception();
												}
											}
										};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(mc);
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

		[Test]
		public void TestVariableStack()
		{
			IRTreeBuilder.VariableStack stack = new IRTreeBuilder.VariableStack();

			AreEqual(-1, stack.TopIndex);

			// Add two.
			MethodVariable v1 = stack.PushTopVariable();
			AreEqual(0, stack.TopIndex);

			MethodVariable v2 = stack.PushTopVariable();
			AreEqual(1, stack.TopIndex);

			stack.TopIndex = 0;
			MethodVariable v1_perhaps = stack.PopTopVariable();
			AreEqual(v1, v1_perhaps);
			AreEqual(-1, stack.TopIndex);

			stack.TopIndex = 1;
			MethodVariable v2_perhaps = stack.PopTopVariable();
			AreEqual(v2, v2_perhaps);
			AreEqual(0, stack.TopIndex);
		}

		[Test]
		public void TestParseUsingVariableStack()
		{
			MemoryStream il = new MemoryStream();
			BinaryWriter w = new BinaryWriter(il);

			// A hard-coded version of Math.Max.

			// Load "arguments".
			w.Write((byte)OpCodes.Ldc_I4_1.Value);
			w.Write((byte)OpCodes.Ldc_I4_5.Value);

			// offset 2.
			w.Write((byte)OpCodes.Ble_S.Value);
			w.Write((byte)4);

			// offset 4.
			w.Write((byte)OpCodes.Ldc_I4_1.Value);
			w.Write((byte)OpCodes.Br_S.Value);
			w.Write((byte)2);

			// offset 7.
			w.Write((byte)OpCodes.Ldc_I4_5.Value);

			// offset 8.
			w.Write((byte)OpCodes.Ret.Value);

			IRTreeBuilder builder = new IRTreeBuilder();
			ILReader reader = new ILReader(il.ToArray());
			List<MethodVariable> variables = new List<MethodVariable>();
			List<IRBasicBlock> blocks = builder.BuildBasicBlocks(reader, variables, new ReadOnlyCollection<MethodParameter>(new MethodParameter[] {}));

			AreEqual(7, reader.InstructionsRead);
		}

	}
}

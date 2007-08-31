using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
			AreEqual(1, ci.Blocks.Count);
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
			AreEqual(1, ci.Blocks.Count);
		}

		[Test]
		public void TestParseArrayInstantiation()
		{
			BasicTestDelegate del = delegate
			                        	{
			                        		int[] arr = new int[5];
			                        	};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			new TreeDrawer().DrawMethod(ci);
			AreEqual(1, ci.Blocks.Count);
		}

		[Test]
		public void TestParseArrayLoadStore()
		{
			BasicTestDelegate del = delegate
										{
											int[] arr = new int[10];
											int j = arr[1];
											arr[2] = j;
										};
			MethodBase method = del.Method;

			List<IRBasicBlock> blocks = new IRTreeBuilder().BuildBasicBlocks(method);
			new TreeDrawer().DrawMethod(blocks);

			// Check that stelem has been removed/decomposed.
			IRBasicBlock.ForeachTreeInstruction(blocks, delegate(TreeInstruction obj) { AreNotEqual(IROpCodes.Stelem, obj.Opcode); });

			// Check that there is an ldelema instruction.
			List<TreeInstruction> ldlist = Algorithms.FindAll(
				IRBasicBlock.EnumerateTreeInstructions(blocks),
			    delegate(TreeInstruction inst) { return inst.Opcode == IROpCodes.Ldelema; });
			AreEqual(1, ldlist.Count);
			AreEqual(1, blocks.Count);
		}


		[Test]
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
		public void TestParseUsingVariableStack()
		{
			ILWriter w = new ILWriter();

			// A hard-coded version of Math.Max.

			// Load "arguments".
			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Ldc_I4_5);

			// offset 2.
			w.WriteOpcode(OpCodes.Ble_S);
			w.WriteByte(3);

			// offset 4.
			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			// offset 7.
			w.WriteOpcode(OpCodes.Ldc_I4_5);

			// offset 8.
			w.WriteOpcode(OpCodes.Ret);

			ILReader reader = new ILReader(w.ToByteArray());
			IRTreeBuilder builder = new IRTreeBuilder();
			List<MethodVariable> variables = new List<MethodVariable>();
			List<IRBasicBlock> blocks = builder.BuildBasicBlocks(reader, variables);

			AreEqual(1, variables.Count);
			AreEqual(7, reader.InstructionsRead);

			// Examine the trees a bit.
			int branchcount = 0;
			int loadcount = 0;
			int storecount = 0;
			int ldccount = 0;
			IRBasicBlock.ForeachTreeInstruction(
				blocks,
				delegate(TreeInstruction obj)
				{
					if (obj.Opcode.FlowControl == FlowControl.Branch ||
						obj.Opcode.FlowControl == FlowControl.Cond_Branch)
					{
						branchcount++;
					}
					else if (obj.Opcode == IROpCodes.Ldloc)
						loadcount++;
					else if (obj.Opcode == IROpCodes.Stloc)
						storecount++;
					else if (obj.Opcode == IROpCodes.Ldc_I4)
						ldccount++;

					if (obj.Opcode == IROpCodes.Br)
					{
						// Check that the Br branches to the inserted load.
						IRBasicBlock target = (IRBasicBlock)obj.Operand;
						AreEqual(IROpCodes.Ldloc, target.Roots[0].Left.Opcode);
					}
				});

			new TreeDrawer().DrawMethod(blocks);

			AreEqual(2, branchcount, "Invalid branch count.");
			AreEqual(1, loadcount, "Invalid load count.");
			AreEqual(2, storecount, "Invalid store count.");
			AreEqual(4, ldccount, "Invalid load constant count.");
		}

		[Test]
		public void TestTypeDerivingForVariableStack()
		{
			// This IL will must introduce a stack variable of type I4 to 
			// hold the integer because of the branch.
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_I4_4);
			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);
			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Pop);

			IRTreeBuilder builder = new IRTreeBuilder();
			List<MethodVariable> vars = new List<MethodVariable>();
			builder.BuildBasicBlocks(w.CreateReader(), vars);
			AreEqual(1, vars.Count, "Invalid variable count.");
			AreEqual(StackTypeDescription.Int32, vars[0].StackType);

			// TODO: Move this test to IRTreeBuilderTest or TypeDeriveTest since we don't use MethodCompiler.
		}
	}
}

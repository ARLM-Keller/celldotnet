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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CellDotNet.Spe;
using NUnit.Framework;



namespace CellDotNet.Intermediate
{
	[TestFixture]
	public class IRTreeBuilderTest : UnitTest
	{
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

//			new TreeDrawer().DrawMethod(ci);
		}


		[Test]
		public void TestParsePopOperandIsNull()
		{
			// will have a pop instruktion before the ret.
			Action del = () => Math.Max(1, 2);

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);


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
			Action del = delegate
										{
											int rem;
											Math.DivRem(9, 13, out rem);
											Math.Max(Math.Min(3, 1), 5L);
										};
			MethodCompiler ci = new MethodCompiler(del.Method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
//			new TreeDrawer().DrawMethod(ci);
			AreEqual(1, ci.Blocks.Count);
		}

		[Test]
		public void TestParseObjectInstantiationAndInstanceMethodCall()
		{
			Action del = delegate
										{
											ArrayList list = new ArrayList(34);
											list.Clear();
										};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);
			AreEqual(1, ci.Blocks.Count);
		}

		[Test]
		public void TestParseArrayInstantiation()
		{
			Action del = delegate
			                        	{
			                        		int[] arr = new int[5];
			                        	};
			MethodBase method = del.Method;
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			AreEqual(1, ci.Blocks.Count);
		}

		[Test]
		public void TestParseArrayLoadStore()
		{
			Action del = delegate
										{
											int[] arr = new int[10];
											int j = arr[1];
											arr[2] = j;
										};
			MethodBase method = del.Method;

			List<IRBasicBlock> blocks = new IRTreeBuilder().BuildBasicBlocks(method);

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
			Action del = delegate
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

			new IRTreeBuilder().BuildBasicBlocks(del.Method);
		}

		[Test]
		public void TestParseBranchesDEBUG()
		{
			Action del = delegate
			                        	{
			                        		{
			                        			throw new Exception();
			                        		}
			                        	};

			new IRTreeBuilder().BuildBasicBlocks(del.Method);
		}

		[Test]
		public void TestParseBranchBasic()
		{
			Action del = delegate
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

			new IRTreeBuilder().BuildBasicBlocks(del.Method);
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
			AreEqual(4, blocks.Count);

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
			w.WriteByte(0);
//			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Pop);

			IRTreeBuilder builder = new IRTreeBuilder();
			List<MethodVariable> vars = new List<MethodVariable>();
			builder.BuildBasicBlocks(w.CreateReader(), vars);
			AreEqual(1, vars.Count, "Invalid variable count.");
			AreEqual(StackTypeDescription.Int32, vars[0].StackType);

			// TODO: Move this test to IRTreeBuilderTest or TypeDeriveTest since we don't use MethodCompiler.
		}

//		static private T ReturnSame<T>(T value)
//		{
//			return value;
//		}

//		[Test]
//		public void TestInlining()
//		{
//			Converter<int, int> del = delegate(int input) { return ReturnSame(input + 5); };
//
//			CompileContext cc = new CompileContext(del.Method);
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
//			AreEqual(1, cc.Methods.Count);
//
//			if (!SpeContext.HasSpeHardware)
//				return;
//
//			using (SpeContext sc = new SpeContext())
//			{
//				object rv = sc.RunProgram(cc, 5);
//				AreEqual(10, (int) rv);
//			}
//		}
	}
}
#endif
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using NUnit.Framework;



namespace CellDotNet
{
	[TestFixture]
	public class ILOpCodeExecutionTest : UnitTest
	{
		private delegate int SimpleDelegateReturn();

		[Test]
		public void Test_Call()
		{
			SimpleDelegateReturn del1 = delegate() { return SpuMath.Max(4, 99); };

			CompileContext cc = new CompileContext(del1.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			new TreeDrawer().DrawMethods(cc);

			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();

				int returnValue = ctx.DmaGetValue<int>(cc.ReturnValueAddress);

				AreEqual(99, returnValue, "Function call returned a wrong value.");
			}
		}

		[Test]
		public void Test_Ret()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 7);
		}

		/// <summary>
		/// NOTE: this function requires short form branch instruction as argument.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="i1"></param>
		/// <param name="i2"></param>
		/// <param name="branch"></param>
		public void ConditionalBranchTest(OpCode opcode, int i1, int i2, bool branch)
		{
			ILWriter w = new ILWriter();

			// Load constants.
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);

			// Conditionally branch 3 bytes.
			w.WriteOpcode(opcode);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			if (branch)
				TestExecution(w, 2);
			else
				TestExecution(w, 1);
		}

		/// <summary>
		/// NOTE: this function requires short form branch instruction as argument.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="i1"></param>
		/// <param name="i2"></param>
		/// <param name="branch"></param>
		public void ConditionalBranchTest(OpCode opcode, float i1, float i2, bool branch)
		{
			ILWriter w = new ILWriter();

			// Load constants.
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(i1);
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(i2);

			// Conditionally branch 3 bytes.
			w.WriteOpcode(opcode);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			if (branch)
				TestExecution(w, 2);
			else
				TestExecution(w, 1);
		}

		[Test]
		public void Test_Br()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Ldc_I4_3);

			// 0x02
			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(2);

			// 0x04
			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			// 0x06
			w.WriteOpcode(OpCodes.Beq_S);
			w.WriteByte(-4);

			// 0x08
			w.WriteOpcode(OpCodes.Ldc_I4_2);
			
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 2);
		}

		[Test]
		public void Test_Br_Simple()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_3);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(0);

//			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 3);
		}

		[Test]
		public void Test_Brtrue()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Ceq);

			w.WriteOpcode(OpCodes.Brtrue_S);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 2);

			w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ceq);

			w.WriteOpcode(OpCodes.Brtrue_S);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 1);
		}

		[Test]
		public void Test_Brfalse()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Ceq);

			w.WriteOpcode(OpCodes.Brfalse_S);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 1);

			w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_1);
			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ceq);

			w.WriteOpcode(OpCodes.Brfalse_S);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 2);
		}

		[Test]
		public void Test_Beq()
		{
			ConditionalBranchTest(OpCodes.Beq_S, 2, 2, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Beq_S, 2f, 2f, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5f, 2f, false);
		}

		[Test]
		public void Test_Bne_Un()
		{
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2, 2, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2f, 2f, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5f, 2f, true);
		}

		[Test]
		public void Test_Bge()
		{
			ConditionalBranchTest(OpCodes.Bge_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2, 5, false);
			ConditionalBranchTest(OpCodes.Bge_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Bgt()
		{
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Ble()
		{
			ConditionalBranchTest(OpCodes.Ble_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2, 5, true);
			ConditionalBranchTest(OpCodes.Ble_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Blt()
		{
			ConditionalBranchTest(OpCodes.Blt_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2, 5, true);
			ConditionalBranchTest(OpCodes.Blt_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Bge_Un()
		{
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2, 5, false);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Bgt_Un()
		{
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Ble_Un()
		{
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2, 5, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Blt_Un()
		{
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 2, 5, true);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Ldc_I4()
		{
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(0xabcdef);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 0xabcdef);
		}

		[Test]
		public void Test_Add_I4()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ldc_I4_3);
			w.WriteOpcode(OpCodes.Add);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 10);
		}

		[Test]
		public void Test_Sub_I4()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ldc_I4_3);
			w.WriteOpcode(OpCodes.Sub);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 4);
		}

		[Test]
		public void Test_Mul_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Mul, 5, 3, 15);
		}

		[Test]
		public void Test_Ldc_R4()
		{
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(4.5f);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 4.5f);
		}

		[Test]
		public void Test_Add_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Add, 3.5f, 4f, 7.5f);
		}

		[Test]
		public void Test_Sub_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Sub, 5.5f, 4f, 1.5f);
		}

		[Test]
		public void Test_Mul_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Mul, 3.5f, 4f, 3.5f * 4f);
		}

		[Test]
		public void Test_Conv_I4_R4()
		{
			ILWriter w = new ILWriter();

			int i = 5;
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i);
			w.WriteOpcode(OpCodes.Conv_R4);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, (float) i);
		}

		[Test]
		public void Test_And()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.And, 0x0f0, 0xf00, 0x000);
			ExecuteAndVerifyBinaryOperator(OpCodes.And, 0x0ff, 0xff0, 0x0f0);
		}

		[Test]
		public void Test_Or()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Or, 0xf00, 0x0f0, 0xff0);
		}

		[Test]
		public void Test_Shl()
		{
			const int num = 0x300f0; // Uses both halfwords.
			ExecuteAndVerifyBinaryOperator(OpCodes.Shl, num, 5, num << 5);
		}

		[Test]
		public void Test_Xor()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Xor, 0xff0, 0x0f0, 0xf00);
		}
		
		[Test]
		public void Test_Ceq_I4()
		{
//			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5, 3, 0);
			ExecuteAndVerifyComparator(OpCodes.Ceq, 5, 5, 1);
		}

		[Test]
		public void Test_Cgt_I4()
		{
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 3, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 7, 0);
		}

		[Test]
		public void Test_Cgt_R4()
		{
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5f, 3f, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5f, 5f, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5f, 7f, 0);
		}

		[Test, Ignore]
		public void Test_Cgt_R8()
		{
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 3, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt, 5, 7, 0);
		}

		[Test]
		public void Test_Clt_I4()
		{
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 3, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 7, 1);
		}

		[Test]
		public void Test_Clt_R4()
		{
			ExecuteAndVerifyComparator(OpCodes.Clt, 5f, 3f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5f, 5f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5f, 7f, 1);
		}

		[Test, Ignore]
		public void Test_Clt_R8()
		{
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 3, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt, 5, 7, 1);
		}

		[Test]
		public void Test_Cgt_Un_I4()
		{
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -3, 5, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -1, -7, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -7, -7, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -7, -1, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 7, -3, 0);

			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5, 3, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5, 7, 0);
		}

		[Test]
		public void Test_Clt_Un_I4()
		{
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -3, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -1, -7, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -7, -7, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -7, -1, 1);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 7, -3, 1);

			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5, 3, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5, 5, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5, 7, 1);
		}

		[Test]
		public void Test_Cgt_Un_R4()
		{
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -3f, 5f, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -1f, -7f, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -7f, -7f, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, -7f, -1f, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 7f, -3f, 1);

			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5f, 3f, 1);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5f, 5f, 0);
			ExecuteAndVerifyComparator(OpCodes.Cgt_Un, 5f, 7f, 0);
		}

		[Test]
		public void Test_Clt_Un_R4()
		{
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -3f, 5f, 1);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -1f, -7f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -7f, -7f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, -7f, -1f, 1);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 7f, -3f, 0);

			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5f, 3f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5f, 5f, 0);
			ExecuteAndVerifyComparator(OpCodes.Clt_Un, 5f, 7f, 1);
		}

		private delegate int DivDelegate(int d1, int d2);

		[Test]
		public void Test_Div_Un()
		{
			DivDelegate fun =
				delegate(int d1, int d2) { return (int)SpuMath.Div_Un((uint)d1, (uint)d2); };

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1 / 1, (int)SpeContext.UnitTestRunProgram(cc, 1, 1), "1 / 1");
			AreEqual(0 / 1, (int)SpeContext.UnitTestRunProgram(cc, 0, 1), "0 / 1");
			AreEqual(17 / 7, (int)SpeContext.UnitTestRunProgram(cc, 17, 7), "17 / 7");
			AreEqual(14 / 7, (int)SpeContext.UnitTestRunProgram(cc, 14, 7), "14 / 7");
			AreEqual(4 / 7, (int)SpeContext.UnitTestRunProgram(cc, 4, 7), "4 / 7");
			AreEqual(42 / 42, (int)SpeContext.UnitTestRunProgram(cc, 42, 42), "42 / 42");
			AreEqual(52907 / 432, (int)SpeContext.UnitTestRunProgram(cc, 52907, 432), "52907 / 432");
		}

		[Test]
		public void Test_Div()
		{
			DivDelegate fun =
				delegate(int d1, int d2) { return SpuMath.Div(d1, d2); };

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1 / 1, (int)SpeContext.UnitTestRunProgram(cc, 1, 1), "");
			AreEqual(0 / 1, (int)SpeContext.UnitTestRunProgram(cc, 0, 1), "");
			AreEqual(17 / 7, (int)SpeContext.UnitTestRunProgram(cc, 17, 7), "");
			AreEqual(14 / 7, (int)SpeContext.UnitTestRunProgram(cc, 14, 7), "");
			AreEqual(-4 / 7, (int)SpeContext.UnitTestRunProgram(cc, -4, 7), "");
			AreEqual(42 / -42, (int)SpeContext.UnitTestRunProgram(cc, 42, -42), "");
			AreEqual(-52907 / -432, (int)SpeContext.UnitTestRunProgram(cc, -52907, -432), "");
		}

		[Test]
		public void Test_Rem_Un()
		{
			DivDelegate fun =
				delegate(int d1, int d2) { return (int)SpuMath.Rem_Un((uint)d1, (uint)d2); };

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1 % 1, (int)SpeContext.UnitTestRunProgram(cc, 1, 1), "");
			AreEqual(0 % 1, (int)SpeContext.UnitTestRunProgram(cc, 0, 1), "");
			AreEqual(17 % 7, (int)SpeContext.UnitTestRunProgram(cc, 17, 7), "");
			AreEqual(14 % 7, (int)SpeContext.UnitTestRunProgram(cc, 14, 7), "");
			AreEqual(4 % 7, (int)SpeContext.UnitTestRunProgram(cc, 4, 7), "");
			AreEqual(42 % 42, (int)SpeContext.UnitTestRunProgram(cc, 42, 42), "");
			AreEqual(52907 % 432, (int)SpeContext.UnitTestRunProgram(cc, 52907, 432), "");
		}

		[Test]
		public void Test_Rem()
		{
			DivDelegate fun =
				delegate(int d1, int d2) { return SpuMath.Rem(d1, d2); };

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1 % 1, (int)SpeContext.UnitTestRunProgram(cc, 1, 1), "");
			AreEqual(0 % 1, (int)SpeContext.UnitTestRunProgram(cc, 0, 1), "");
			AreEqual(17 % 7, (int)SpeContext.UnitTestRunProgram(cc, 17, 7), "");
			AreEqual(14 % 7, (int)SpeContext.UnitTestRunProgram(cc, 14, 7), "");
			AreEqual(-4 % 7, (int)SpeContext.UnitTestRunProgram(cc, -4, 7), "");
			AreEqual(42 % -42, (int)SpeContext.UnitTestRunProgram(cc, 42, -42), "");
			AreEqual(-52907 % -432, (int)SpeContext.UnitTestRunProgram(cc, -52907, -432), "");
		}

		private delegate float DivFloatDelegate(float d1, float d2);

		private static void Test_Div_Float_Helper(CompileContext cc, float dividend, float divisor, float error)
		{
			float result;
			float correct;
			int resultint;
			int correctint;

			correct = dividend / divisor;
			result = (float)SpeContext.UnitTestRunProgram(cc, dividend, divisor);
			resultint = Utilities.ReinterpretAsInt(result);
			correctint = Utilities.ReinterpretAsInt(correct);
//			Console.WriteLine("{0} / {1} Mono: {2} SPU: {3}4", dividend, divisor, correct, result);

			Utilities.AssertWithinLimits(result, correct, error, "");

			Utilities.PretendVariableIsUsed(resultint);
			Utilities.PretendVariableIsUsed(correctint);
		}

		[Test]
		public void Test_Div_Float()
		{
			DivFloatDelegate fun =
				delegate(float d1, float d2) { return d1/d2; };

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			float error = 0.00001f;

			Test_Div_Float_Helper(cc, 17f, 7f, error);
			Test_Div_Float_Helper(cc, 14f, 7f, error);
			Test_Div_Float_Helper(cc, 4f, 7f, error);
			Test_Div_Float_Helper(cc, 42f, 42f, error);
			Test_Div_Float_Helper(cc, 52907f, 432f, error);

			Test_Div_Float_Helper(cc, 0f, 1f, error);
			Test_Div_Float_Helper(cc, 1f, 1f, error);
			Test_Div_Float_Helper(cc, -1f, 1f, error);
			Test_Div_Float_Helper(cc, 1f, -11f, error);
			Test_Div_Float_Helper(cc, -1f, -1f, error);

			Test_Div_Float_Helper(cc, 0f, 543f, error);
			Test_Div_Float_Helper(cc, 1123f, 1423f, error);
			Test_Div_Float_Helper(cc, -643f, 234f, error);
			Test_Div_Float_Helper(cc, 64323f, -53441f, error);
			Test_Div_Float_Helper(cc, -6453f, -4567f, error);
		}

		[Test]
		public void Test_Dub()
		{
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Dup);
			w.WriteOpcode(OpCodes.Add);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, 14);
		}

		[Test]
		public void Test_Neg_Int()
		{
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Neg);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, -7);
		}

		[Test]
		public void Test_Neg_Float()
		{
			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(3.14f);
			w.WriteOpcode(OpCodes.Neg);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, -3.14f);
		}

		private delegate double DoubleDelegate();

		[Test, Ignore]
		public void Test_Ldc_R8()
		{
			const double magicnumber = -4203.57;
			DoubleDelegate del = delegate {return magicnumber;};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if(!SpeContext.HasSpeHardware)
				return;

			double resutl = (double)new SpeContext().RunProgram(cc);
			AreEqual(magicnumber, resutl);
		}


		private static int f1()
		{
			int i = 5;
			f2(ref i);
			return i;
		}

		private static void f2(ref int a)
		{
			a = a + 1;
		}

		[Test]
		public void TestRefArgumentTest()
		{
			SimpleDelegateReturn del1 = f1;


			CompileContext cc = new CompileContext(del1.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();

				int returnValue = ctx.DmaGetValue<int>(cc.ReturnValueAddress);

				AreEqual(6, returnValue, "Function call with ref argument returned a wrong value.");
			}
		}

		public void ExecuteAndVerifyComparator(OpCode opcode, int i1, int i2, int expectedValue)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, expectedValue);
		}

		public void ExecuteAndVerifyComparator(OpCode opcode, float i1, float i2, int expectedValue)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(i1);
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(i2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, expectedValue);
		}

		public void ExecuteAndVerifyBinaryOperator(OpCode opcode, int i1, int i2, int expectedValue)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, expectedValue);
		}

		public void ExecuteAndVerifyBinaryOperator(OpCode opcode, float f1, float f2, float expectedValue)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(f1);
			w.WriteOpcode(OpCodes.Ldc_R4);
			w.WriteFloat(f2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, expectedValue);
		}

		private static void TestExecution<T>(ILWriter ilcode, T expectedValue) where T : struct
		{
			RegisterSizedObject returnAddressObject = new RegisterSizedObject("ReturnObject");

			IRTreeBuilder builder = new IRTreeBuilder();
			List<MethodVariable> vars = new List<MethodVariable>();

			List<IRBasicBlock> basicBlocks;
			try
			{
				basicBlocks = builder.BuildBasicBlocks(ilcode.CreateReader(), vars);
			}
			catch (ILParseException)
			{
				Console.WriteLine("IL causing ILParseException:");
				DumpILToConsole(ilcode);
				throw;
			}

			foreach (MethodVariable var in vars)
			{
				var.VirtualRegister = new VirtualRegister();
			}

//			new TreeDrawer().DrawMethod(basicBlocks);

			RecursiveInstructionSelector sel = new RecursiveInstructionSelector();

			ReadOnlyCollection<MethodParameter> par = new ReadOnlyCollection<MethodParameter>(new List<MethodParameter>());

			ManualRoutine spum = new ManualRoutine(false, "opcodetester");

			SpecialSpeObjects specialSpeObjects = new SpecialSpeObjects();
			specialSpeObjects.SetMemorySettings(256 * 1024 - 0x20, 8 * 1024 - 0x20, 128 * 1024, 118 * 1024);

			// Currently (20070917) the linear register allocator only uses calle saves registers, so
			// it will always spill some.
			spum.WriteProlog(10, specialSpeObjects.StackOverflow);

			sel.GenerateCode(basicBlocks, par, spum.Writer);

			spum.WriteEpilog();

			// TODO Det håndteres muligvis ikke virtuelle moves i SimpleRegAlloc.
			// NOTE: køre ikke på prolog og epilog.
			{
				int nextspillOffset = 3;
				new LinearRegisterAllocator().Allocate(spum.Writer.BasicBlocks.GetRange(1, spum.Writer.BasicBlocks.Count - 2),
											  delegate { return nextspillOffset++; }, null);
				
			}
			RegAllocGraphColloring.RemoveRedundantMoves(spum.Writer.BasicBlocks);

			SpuInitializer spuinit = new SpuInitializer(spum, returnAddressObject, null, 0, specialSpeObjects.StackPointerObject, specialSpeObjects.NextAllocationStartObject, specialSpeObjects.AllocatableByteCountObject);

			List<ObjectWithAddress> objectsWithAddresss = new List<ObjectWithAddress>();

			objectsWithAddresss.Add(spuinit);
			objectsWithAddresss.Add(spum);
			objectsWithAddresss.AddRange(specialSpeObjects.GetAll());
			objectsWithAddresss.Add(returnAddressObject);

			int codeByteSize = CompileContext.LayoutObjects(objectsWithAddresss);

			foreach (ObjectWithAddress o in objectsWithAddresss)
			{
				SpuDynamicRoutine dynamicRoutine = o as SpuDynamicRoutine;
				if(dynamicRoutine != null)
					dynamicRoutine.PerformAddressPatching();
			}

//			Disassembler.DisassembleToConsole(objectsWithAddresss);	

			int[] code = new int[codeByteSize/4];
			CompileContext.CopyCode(code, new SpuDynamicRoutine[] { spuinit, spum });

			const int TotalSpeMem = 256*1024;
			const int StackPointer = 256*1024 - 32;
			const int StackSize = 8*1024;
			specialSpeObjects.SetMemorySettings(StackPointer, StackSize, codeByteSize, TotalSpeMem - codeByteSize - StackSize);

			// We only need to write to the preferred slot.
			code[specialSpeObjects.AllocatableByteCountObject.Offset/4] = specialSpeObjects.AllocatableByteCount;
			code[specialSpeObjects.NextAllocationStartObject.Offset/4] = specialSpeObjects.NextAllocationStart;

			code[specialSpeObjects.StackPointerObject.Offset/4] = specialSpeObjects.InitialStackPointer;
			code[specialSpeObjects.StackPointerObject.Offset/4 + 1] = specialSpeObjects.StackSize;
			// NOTE: SpuAbiUtilities.WriteProlog() is dependending on that the two last words being >= stackSize.
			code[specialSpeObjects.StackPointerObject.Offset/4 + 2] = specialSpeObjects.StackSize;
			code[specialSpeObjects.StackPointerObject.Offset/4 + 3] = specialSpeObjects.StackSize;


			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();

				T returnValue = ctx.DmaGetValue<T>((LocalStorageAddress) returnAddressObject.Offset);

				AreEqual(expectedValue, returnValue, "SPU delegate execution returned a wrong value.");
			}
		}

		private static void DumpILToConsole(ILWriter ilcode)
		{
			// Dump readable IL.
			StringWriter sw = new StringWriter();
			sw.WriteLine("Parsed IL:");
			try
			{
				ILReader r = ilcode.CreateReader();
				while (r.Read())
					sw.WriteLine("{0:x4}: {1} {2}", r.Offset, r.OpCode.Name, r.Operand);
			}
			catch (ILParseException) { }

			// Dump bytes.
			sw.WriteLine("IL bytes:");
			byte[] il = ilcode.ToByteArray();
			for (int offset = 0; offset < il.Length; offset++)
			{
				if (offset % 4 == 0)
				{
					if (offset > 0)
						sw.WriteLine();
					sw.Write("{0:x4}: ", offset);
				}

				sw.Write(" " + il[offset].ToString("x2"));
			}
			Console.WriteLine(sw.GetStringBuilder());
		}
	}
}
#endif
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
using CellDotNet.Intermediate;
using CellDotNet.Spe;
using JetBrains.Annotations;
using NUnit.Framework;
using System.Linq;


namespace CellDotNet
{
	[TestFixture]
	public class ILOpCodeExecutionTest : UnitTest
	{
		[Test]
		public void Test_Call()
		{
			Func<int> del1 = () => SpuMath.Max(4, 99);

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

		/// <summary>
		/// NOTE: this function requires short form branch instruction as argument.
		/// </summary>
		public void ConditionalBranchTest(OpCode opcode, double i1, double i2, bool branch)
		{
			ILWriter w = new ILWriter();

			// Load constants.
			w.WriteOpcode(OpCodes.Ldc_R8);
			w.WriteDouble(i1);
			w.WriteOpcode(OpCodes.Ldc_R8);
			w.WriteDouble(i2);

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

		private void ExecuteAndVerifyUnary<TInput, TOutput>(OpCode opcode, TInput input, TOutput expectedResult) 
			where TInput : struct 
			where TOutput : struct
		{
			ILWriter w = new ILWriter();
			switch (Type.GetTypeCode(typeof(TInput)))
			{
				case TypeCode.Int32:
					w.WriteOpcode(OpCodes.Ldc_I4);
					w.WriteInt32((input as int?).Value);
					break;
				case TypeCode.Single:
					w.WriteOpcode(OpCodes.Ldc_R4);
					w.WriteFloat((input as float?).Value);
					break;
				case TypeCode.Double:
					w.WriteOpcode(OpCodes.Ldc_R8);
					w.WriteDouble((input as double?).Value);
					break;
				default:
					throw new ArgumentException();
			}

			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, expectedResult);
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
		public void Test_Beq_I4()
		{
			ConditionalBranchTest(OpCodes.Beq_S, 2, 2, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5, 2, false);
		}

		[Test]
		public void Test_Beq_R4()
		{
			ConditionalBranchTest(OpCodes.Beq_S, 2f, 2f, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5f, 2f, false);
		}

		[Test]
		public void Test_Bne_Un_I4()
		{
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2, 2, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5, 2, true);
		}

		[Test]
		public void Test_Bne_Un_R4()
		{
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2f, 2f, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5f, 2f, true);
		}

		[Test]
		public void Test_Bge_I4()
		{
			ConditionalBranchTest(OpCodes.Bge_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2, 5, false);
		}

		[Test]
		public void Test_Bge_R4()
		{
			ConditionalBranchTest(OpCodes.Bge_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Bgt_I4()
		{
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2, 5, false);
		}

		[Test]
		public void Test_Bgt_R4()
		{
			ConditionalBranchTest(OpCodes.Bgt_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Ble_I4()
		{
			ConditionalBranchTest(OpCodes.Ble_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2, 5, true);
		}

		[Test]
		public void Test_Ble_R4()
		{
			ConditionalBranchTest(OpCodes.Ble_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Blt_I4()
		{
			ConditionalBranchTest(OpCodes.Blt_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2, 5, true);
		}

		[Test]
		public void Test_Blt_R4()
		{
			ConditionalBranchTest(OpCodes.Blt_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Bge_Un_I4()
		{
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2, 5, false);
		}

		[Test]
		public void Test_Bge_Un_R4()
		{
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Bgt_Un_I4()
		{
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2, 5, false);
		}

		[Test]
		public void Test_Bgt_Un_R4()
		{
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5f, 2f, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5f, 5f, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2f, 5f, false);
		}

		[Test]
		public void Test_Ble_Un_I4()
		{
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2, 5, true);
		}

		[Test]
		public void Test_Ble_Un_R4()
		{
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5f, 2f, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5f, 5f, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2f, 5f, true);
		}

		[Test]
		public void Test_Blt_Un_I4()
		{
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 2, 5, true);
		}

		[Test]
		public void Test_Blt_Un_R4()
		{
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
			w.WriteFloat(-4324534.523226f);
			w.WriteOpcode(OpCodes.Ret);

			TestExecution(w, -4324534.523226f);
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
		public void Test_ConvR4_I4()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, 5, 5f);
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, -5, -5f);
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, 0, 0f);
		}

		[Test]
		public void Test_ConvR4_R8()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, 5d, 5f);
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, -5d, -5f);
			ExecuteAndVerifyUnary(OpCodes.Conv_R4, 0d, 0f);
		}

		[Test]
		public void Test_ConvR8_I4()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, 5, 5d);
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, -5, -5d);
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, 0, 0d);
		}

		[Test]
		public void Test_ConvR8_R4()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, 5f, 5d);
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, -5f, -5d);
			ExecuteAndVerifyUnary(OpCodes.Conv_R8, 0f, 0d);
		}

		[Test]
		public void Test_ConvI4_R4()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 5f, 5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, -5f, -5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 0f, 0);

			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 5.8f, 5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, -5.8f, -5);
		}

		[Test]
		public void Test_ConvI4_R8()
		{
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 5d, 5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, -5d, -5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 0d, 0);

			ExecuteAndVerifyUnary(OpCodes.Conv_I4, 5.8d, 5);
			ExecuteAndVerifyUnary(OpCodes.Conv_I4, -5.8d, -5);
		}

		[Test]
		public void Test_And_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.And, 0x0f0, 0xf00, 0x000);
			ExecuteAndVerifyBinaryOperator(OpCodes.And, 0x0ff, 0xff0, 0x0f0);
		}

		[Test]
		public void Test_Or_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Or, 0xf00, 0x0f0, 0xff0);
		}

		[Test]
		public void Test_Shl_I4()
		{
			const int num = 0x300f0; // Uses both halfwords.
			ExecuteAndVerifyBinaryOperator(OpCodes.Shl, num, 5, num << 5);
		}

		[Test]
		public void Test_Xor_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Xor, 0xff0, 0x0f0, 0xf00);
		}
		
		[Test]
		public void Test_Ceq_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5, 5, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5, -5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5, 4, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 0, 0, 1);
		}

		[Test]
		public void Test_Ceq_O()
		{
			Func<bool> del;
            
			del = () =>
			      	{
			      		object o1 = null;
			      		object o2 = null;
			      		return o1 == o2;
			      	};
			IsTrue((bool) SpeContext.UnitTestRunProgram(del), "null - null");

			del = () =>
			      	{
			      		object o1 = new object();
			      		object o2 = o1;
			      		return o1 == o2;
			      	};
			IsTrue((bool)SpeContext.UnitTestRunProgram(del), "obj1 - obj1");

			del = () =>
			      	{
			      		object o1 = new object();
			      		return o1 == null;
			      	};
			IsFalse((bool)SpeContext.UnitTestRunProgram(del), "obj1 - null");

			del = () =>
			      	{
			      		object o1 = new object();
						return null == o1;
			      	};
			IsFalse((bool)SpeContext.UnitTestRunProgram(del), "null - obj1");

			del = () =>
			      	{
			      		object o1 = new object();
			      		object o2 = new object();
						return o1 == o2;
			      	};
			IsFalse((bool)SpeContext.UnitTestRunProgram(del), "obj1 - obj2");
		}

		[Test]
		public void Test_Ceq_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5f, 5f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5f, 5.1f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5f, -5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5f, 4f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 0f, 0f, 1);
		}

		[Test, Ignore("This is not going to work any time soon.")]
		public void Test_Ceq_R4_UnorderedNumbers()
		{
			// TODO Figure out if this should work correctly, in which case tests like these should also be there for the other comparisons.
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, float.NaN, float.NaN, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, float.NaN, float.NegativeInfinity, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, float.NaN, float.PositiveInfinity, 0);
		}

		[Test]
		public void Test_Ceq_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5d, 5d, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5d, 5.1d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5d, -5d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5d, 4d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 0d, 0d, 1);
		}

		[Test]
		public void Test_Cgt_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 3, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 7, 0);
		}

		[Test]
		public void Test_Cgt_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5f, 3f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5f, 5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5f, 7f, 0);
		}

		[Test]
		public void Test_Cgt_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5.0, 3.0, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5.0, 5.0, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5.0, 7.0, 0);
		}

		[Test]
		public void Test_Clt_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5, 3, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5, 7, 1);
		}

		[Test]
		public void Test_Clt_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5f, 3f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5f, 5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5f, 7f, 1);
		}

		[Test]
		public void Test_Clt_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5d, 3d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5d, 5d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt, 5d, 7d, 1);
		}

		[Test]
		public void Test_Cgt_Un_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -3, 5, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -1, -7, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7, -7, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7, -1, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 7, -3, 0);

			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5, 3, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5, 7, 0);
		}

		[Test]
		public void Test_Clt_Un_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -3, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -1, -7, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7, -7, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7, -1, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 7, -3, 1);

			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5, 3, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5, 7, 1);
		}

		[Test]
		public void Test_Cgt_Un_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -3f, 5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -1f, -7f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7f, -7f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7f, -1f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 7f, -3f, 1);

			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, 3f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, 5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, 7f, 0);
		}

		[Test, Ignore("This is not going to work any time soon.")]
		public void Test_Cgt_Un_R4_UnorderedNumbers()
		{
			// TODO Figure out if this should work correctly, in which case tests like these should also be there for the other comparisons.
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, float.NaN, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, float.PositiveInfinity, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5f, float.NegativeInfinity, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 0f, float.NegativeInfinity, 1);
		}

		[Test]
		public void Test_Cgt_Un_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -3d, 5d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -1d, -7d, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7d, -7d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, -7d, -1d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 7d, -3d, 1);

			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5d, 3d, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5d, 5d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt_Un, 5d, 7d, 0);
		}

		[Test]
		public void Test_Clt_Un_R4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -3f, 5f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -1f, -7f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7f, -7f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7f, -1f, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 7f, -3f, 0);

			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5f, 3f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5f, 5f, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5f, 7f, 1);
		}
		[Test]

		public void Test_Clt_Un_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -3d, 5d, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -1d, -7d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7d, -7d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, -7d, -1d, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 7d, -3d, 0);

			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5d, 3d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5d, 5d, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Clt_Un, 5d, 7d, 1);
		}

		[Test]
		public void Test_Div_Un()
		{
			Func<uint, uint, uint> fun = (u1, u2) => u1/u2;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual((uint)1 / 1, (uint)SpeContext.UnitTestRunProgram(cc, (uint)1, (uint)1), "1 / 1");
			AreEqual((uint)0 / 1, (uint)SpeContext.UnitTestRunProgram(cc, (uint)0, (uint)1), "0 / 1");
			AreEqual((uint)17 / 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)17, (uint)7), "17 / 7");
			AreEqual((uint)14 / 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)14, (uint)7), "14 / 7");
			AreEqual((uint)4 / 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)4, (uint)7), "4 / 7");
			AreEqual((uint)42 / 42, (uint)SpeContext.UnitTestRunProgram(cc, (uint)42, (uint)42), "42 / 42");
			AreEqual((uint)52907 / 432, (uint)SpeContext.UnitTestRunProgram(cc, (uint)52907, (uint)432), "52907 / 432");
		}

		[Test]
		public void Test_Div_I4()
		{
			Func<int, int, int> fun = (x, y) => x / y;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1 / 1, (int)SpeContext.UnitTestRunProgram(cc, 1, 1));
			AreEqual(0 / 1, (int)SpeContext.UnitTestRunProgram(cc, 0, 1));
			AreEqual(17 / 7, (int)SpeContext.UnitTestRunProgram(cc, 17, 7));
			AreEqual(14 / 7, (int)SpeContext.UnitTestRunProgram(cc, 14, 7));
			AreEqual(-4 / 7, (int)SpeContext.UnitTestRunProgram(cc, -4, 7));
			AreEqual(42 / -42, (int)SpeContext.UnitTestRunProgram(cc, 42, -42));
			AreEqual(-52907 / -432, (int)SpeContext.UnitTestRunProgram(cc, -52907, -432));
		}

		[Test]
		public void Test_Rem_Un_I4()
		{
			Func<uint, uint, uint> fun = SpuMath.Rem_Un;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual((uint)1 % 1, (uint)SpeContext.UnitTestRunProgram(cc, (uint)1, (uint)1));
			AreEqual((uint)0 % 1, (uint)SpeContext.UnitTestRunProgram(cc, (uint)0, (uint)1));
			AreEqual((uint)17 % 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)17, (uint)7));
			AreEqual((uint)14 % 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)14, (uint)7));
			AreEqual((uint)4 % 7, (uint)SpeContext.UnitTestRunProgram(cc, (uint)4, (uint)7));
			AreEqual((uint)42 % 42, (uint)SpeContext.UnitTestRunProgram(cc, (uint)42, (uint)42));
			AreEqual((uint)52907 % 432, (uint)SpeContext.UnitTestRunProgram(cc, (uint)52907, (uint)432));
		}

		[Test]
		public void Test_Rem_I4()
		{
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 1, 1, 1 % 1);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 1, 1, 1 % 1);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 0, 1, 0 % 1);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 17, 7, 17 % 7);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 14, 7, 14 % 7);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, -4, 7, -4 % 7);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, 42, -42, 42 % -42);
			ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCodes.Rem, -52907, -432, -52907 % -432);
		}

		[Test, ExpectedException(typeof(DivideByZeroException))]
		public void Test_Rem_I4_DivideByZeroException()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Rem, 1, 0, 0);
		}

		[Test, ExpectedException(typeof(ArithmeticException))]
		public void Test_Rem_I4_ArithmeticException()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Rem, int.MinValue, -1, 0);
		}

		[Test]
		public void Test_Rem_R4()
		{
			Func<float, float, float> fun = (x, y) => x % y;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1f % 1f, (float)SpeContext.UnitTestRunProgram(cc, 1f, 1f));
			AreEqual(0f % 1f, (float)SpeContext.UnitTestRunProgram(cc, 0f, 1f));
			AreEqual(17f % 7f, (float)SpeContext.UnitTestRunProgram(cc, 17f, 7f));
			AreEqual(14f % 7f, (float)SpeContext.UnitTestRunProgram(cc, 14f, 7f));
			AreEqual(-4f % 7f, (float)SpeContext.UnitTestRunProgram(cc, -4f, 7f));
			AreEqual(42f % -42f, (float)SpeContext.UnitTestRunProgram(cc, 42f, -42f));
			AreEqual(-52907f % -432f, (float)SpeContext.UnitTestRunProgram(cc, -52907f, -432f));
		}

		[Test]
		public void Test_Rem_R8()
		{
			Func<double, double, double> fun = (x, y) => x % y;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			AreEqual(1d % 1d, (double)SpeContext.UnitTestRunProgram(cc, 1d, 1d));
			AreEqual(0d % 1d, (double)SpeContext.UnitTestRunProgram(cc, 0d, 1d));
			AreEqual(17d % 7d, (double)SpeContext.UnitTestRunProgram(cc, 17d, 7d));
			AreEqual(14d % 7d, (double)SpeContext.UnitTestRunProgram(cc, 14d, 7d));
			AreEqual(-4d % 7d, (double)SpeContext.UnitTestRunProgram(cc, -4d, 7d));
			AreEqual(42d % -42d, (double)SpeContext.UnitTestRunProgram(cc, 42d, -42d));
			AreEqual(-52907d % -432d, (double)SpeContext.UnitTestRunProgram(cc, -52907d, -432d));
		}

		private static void Test_Div_R4_Helper(CompileContext cc, float dividend, float divisor, float error)
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
		public void Test_Div_R4()
		{
			Func<float, float, float> fun = (d1, d2) => d1/d2;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			float error = 0.00001f;

			Test_Div_R4_Helper(cc, 17f, 7f, error);
			Test_Div_R4_Helper(cc, 14f, 7f, error);
			Test_Div_R4_Helper(cc, 4f, 7f, error);
			Test_Div_R4_Helper(cc, 42f, 42f, error);
			Test_Div_R4_Helper(cc, 52907f, 432f, error);

			Test_Div_R4_Helper(cc, 0f, 1f, error);
			Test_Div_R4_Helper(cc, 1f, 1f, error);
			Test_Div_R4_Helper(cc, -1f, 1f, error);
			Test_Div_R4_Helper(cc, 1f, -11f, error);
			Test_Div_R4_Helper(cc, -1f, -1f, error);

			Test_Div_R4_Helper(cc, 0f, 543f, error);
			Test_Div_R4_Helper(cc, 1123f, 1423f, error);
			Test_Div_R4_Helper(cc, -643f, 234f, error);
			Test_Div_R4_Helper(cc, 64323f, -53441f, error);
			Test_Div_R4_Helper(cc, -6453f, -4567f, error);
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
		public void Test_Neg_I4()
		{
			ExecuteAndVerifyUnary(OpCodes.Neg, 7, -7);
			ExecuteAndVerifyUnary(OpCodes.Neg, -100, 100);
			ExecuteAndVerifyUnary(OpCodes.Neg, int.MinValue, int.MinValue);
		}

		[Test]
		public void Test_Neg_R4()
		{
			ExecuteAndVerifyUnary(OpCodes.Neg, 3.14f, -3.14f);
			ExecuteAndVerifyUnary(OpCodes.Neg, -3.14f, 3.14f);
			ExecuteAndVerifyUnary(OpCodes.Neg, float.MinValue, float.MaxValue);
			ExecuteAndVerifyUnary(OpCodes.Neg, float.MaxValue, float.MinValue);
		}

		[Test]
		public void Test_Neg_R8()
		{
			ExecuteAndVerifyUnary(OpCodes.Neg, 3.14d, -3.14d);
			ExecuteAndVerifyUnary(OpCodes.Neg, -3.14d, 3.14d);
			ExecuteAndVerifyUnary(OpCodes.Neg, double.MinValue, double.MaxValue);
			ExecuteAndVerifyUnary(OpCodes.Neg, double.MaxValue, double.MinValue);
		}

		[Test]
		public void Test_Ldc_R8()
		{
			{
				const double magicnumber = -4324534.523226;
				Func<double> del = () => magicnumber;

				double result = (double)SpeContext.UnitTestRunProgram(del);
				AreEqual(magicnumber, result);
			}
			{
				const double magicnumber = double.NegativeInfinity;
				Func<double> del = () => magicnumber;

				double result = (double)SpeContext.UnitTestRunProgram(del);
				AreEqual(magicnumber, result);
			}
		}

		[Test]
		public void Test_Sub_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Sub, 5.5, 4.0, 1.5);
		}

		[Test]
		public void Test_Mul_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Mul, 3.5, 4.0, 3.5 * 4.0);
		}

		[Test]
		public void Test_Add_R8()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Add, 3.5, 4.0, 3.5 * 4.0);
		}

		static void dostuff([NotNull] string s)
		{
			
		}

		private static void Test_Div_R8_Helper(CompileContext cc, double dividend, double divisor, double error)
		{
			string sadsf = null;
			dostuff(sadsf);

			double result;
			double correct;
			long resultlong;
			long correctlong;

			correct = dividend / divisor;
			result = (double)SpeContext.UnitTestRunProgram(cc, dividend, divisor);
			resultlong = Utilities.ReinterpretAsLong(result);
			correctlong = Utilities.ReinterpretAsLong(correct);
			//			Console.WriteLine("{0} / {1} Mono: {2} SPU: {3}4", dividend, divisor, correct, result);

			Utilities.AssertWithinLimits(result, correct, error, "");

			Utilities.PretendVariableIsUsed(resultlong);
			Utilities.PretendVariableIsUsed(correctlong);
		}

		[Test]
		public void Test_Div_R8()
		{
			Func<double, double, double> fun = (d1, d2) => d1 / d2;

			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			double error = 0.00001d;

			Test_Div_R8_Helper(cc, 17d, 7d, error);
			Test_Div_R8_Helper(cc, 14d, 7d, error);
			Test_Div_R8_Helper(cc, 4d, 7d, error);
			Test_Div_R8_Helper(cc, 42d, 42d, error);
			Test_Div_R8_Helper(cc, 52907d, 432d, error);

			Test_Div_R8_Helper(cc, 0d, 1d, error);
			Test_Div_R8_Helper(cc, 1d, 1d, error);
			Test_Div_R8_Helper(cc, -1d, 1d, error);
			Test_Div_R8_Helper(cc, 1d, -11d, error);
			Test_Div_R8_Helper(cc, -1d, -1d, error);

			Test_Div_R8_Helper(cc, 0d, 543d, error);
			Test_Div_R8_Helper(cc, 1123d, 1423d, error);
			Test_Div_R8_Helper(cc, -643d, 234d, error);
			Test_Div_R8_Helper(cc, 64323d, -53441d, error);
			Test_Div_R8_Helper(cc, -6453d, -4567d, error);
		}

		[Test]
		public void Test_Beq_R8()
		{
			ConditionalBranchTest(OpCodes.Beq_S, 2d, 2d, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5d, 2d, false);
		}

		[Test]
		public void Test_Bne_Un_R8()
		{
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2d, 2d, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5d, 2d, true);
		}

		[Test]
		public void Test_Bge_R8()
		{
			ConditionalBranchTest(OpCodes.Bge_S, 5d, 2d, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5d, 5d, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2d, 5d, false);
		}

		[Test]
		public void Test_Bgt_R8()
		{
			ConditionalBranchTest(OpCodes.Bgt_S, 5d, 2d, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5d, 5d, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2d, 5d, false);
		}

		[Test]
		public void Test_Ble_R8()
		{
			ConditionalBranchTest(OpCodes.Ble_S, 5d, 2d, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5d, 5d, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2d, 5d, true);
		}

		[Test]
		public void Test_Blt_R8()
		{
			ConditionalBranchTest(OpCodes.Blt_S, 5d, 2d, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5d, 5d, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2d, 5d, true);
		}

		[Test]
		public void Test_Bge_Un_R8()
		{
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5d, 2d, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5d, 5d, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2d, 5d, false);
		}

		[Test]
		public void Test_Bgt_Un_R8()
		{
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5d, 2d, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5d, 5d, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2d, 5d, false);
		}

		[Test]
		public void Test_Ble_Un_R8()
		{
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5d, 2d, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5d, 5d, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2d, 5d, true);
		}

		[Test]
		public void Test_Blt_Un_R8()
		{
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5d, 2d, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5d, 5d, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 2d, 5d, true);
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
			Func<int> del1 = f1;


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

		public void ExecuteAndVerifyBinaryOperator<T>(OpCode opcode, int i1, int i2, T expectedValue) where T:struct 
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

		public void ExecuteAndVerifyBinaryOperatorWithCompileContext(OpCode opcode, int i1, int i2, object expectedValue)
		{
			DynamicMethod dm = new	DynamicMethod("-test", typeof(int), Type.EmptyTypes);
			ILGenerator gen = dm.GetILGenerator(20);
			gen.Emit(OpCodes.Ldc_I4, i1);
			gen.Emit(OpCodes.Ldc_I4, i2);
			gen.Emit(opcode);
			gen.Emit(OpCodes.Ret);

			int rv = (int) SpeContext.UnitTestRunProgram(new CompileContext(dm));
			AreEqual(expectedValue, rv);
		}

		public void ExecuteAndVerifyBinaryOperator<T>(OpCode opcode, float f1, float f2, T expectedValue) where T:struct 
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

		public void ExecuteAndVerifyBinaryOperator<T>(OpCode opcode, double d1, double d2, T expectedValue) where T:struct
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_R8);
			w.WriteDouble(d1);
			w.WriteOpcode(OpCodes.Ldc_R8);
			w.WriteDouble(d2);
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

			ReadOnlyCollection<MethodParameter> par = new ReadOnlyCollection<MethodParameter>(new List<MethodParameter>());

			ManualRoutine spum = new ManualRoutine(false, "opcodetester");

			SpecialSpeObjects specialSpeObjects = new SpecialSpeObjects();
			specialSpeObjects.SetMemorySettings(256 * 1024 - 0x20, 8 * 1024 - 0x20, 128 * 1024, 118 * 1024);

			// Currently (20070917) the linear register allocator only uses calle saves registers, so
			// it will always spill some.
			spum.WriteProlog(10, specialSpeObjects.StackOverflow);

			RecursiveInstructionSelector sel = new RecursiveInstructionSelector(specialSpeObjects, null);
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
			objectsWithAddresss.AddRange(specialSpeObjects.GetAllObjectsWithStorage());
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

			CompileContext.CopyInitializedData(code, specialSpeObjects.GetAllObjectsWithStorage());
			CompileContext.WriteAssemblyToFile("r8_ceq.s", code, objectsWithAddresss);


			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();

				T returnValue = (T) ctx.DmaGetValue((LocalStorageAddress) returnAddressObject.Offset, typeof (T));

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
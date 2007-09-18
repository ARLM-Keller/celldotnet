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

//		private delegate void SimpleDelegateRefArg(ref int a);

		[Test]
		public void Test_Call()
		{
			SimpleDelegateReturn del1 = delegate() { return Math.Max(4, 99); };

			CompileContext cc = new CompileContext(del1.Method);

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);

//			Disassembler.DisassembleUnconditional(cc, Console.Out);

			cc.PerformProcessing(CompileContextState.S8Complete);

//			Disassembler.DisassembleToConsole(cc);

//			cc.WriteAssemblyToFile(Utilities.GetUnitTestName() + "_asm.s", new ValueType[0]);

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

			Execution(w, 7);
		}

		// NOTE: this function requires short form branch instruction as argument.
		public void ConditionalBranchTest(OpCode opcode, int i1, int i2, bool branch)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);
			w.WriteOpcode(opcode);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);

			w.WriteOpcode(OpCodes.Ret);

			if(branch)
				Execution(w, 2);
			else
				Execution(w, 1);
		}

		[Test]
		public void Test_Br()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(3);

			w.WriteOpcode(OpCodes.Ldc_I4_1);

			w.WriteOpcode(OpCodes.Br_S);
			w.WriteByte(1);

			w.WriteOpcode(OpCodes.Ldc_I4_2);
			
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, 2);
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

			Execution(w, 2);

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

			Execution(w, 1);
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

			Execution(w, 1);

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

			Execution(w, 2);
		}

		[Test]
		public void Test_Beq()
		{
			ConditionalBranchTest(OpCodes.Beq_S, 2, 2, true);
			ConditionalBranchTest(OpCodes.Beq_S, 5, 2, false);
		}

		[Test]
		public void Test_Bne_Un()
		{
			ConditionalBranchTest(OpCodes.Bne_Un_S, 2, 2, false);
			ConditionalBranchTest(OpCodes.Bne_Un_S, 5, 2, true);
		}

		[Test]
		public void Test_Bge()
		{
			ConditionalBranchTest(OpCodes.Bge_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_S, 2, 5, false);
		}

		[Test]
		public void Test_Bgt()
		{
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_S, 2, 5, false);
		}

		[Test]
		public void Test_Ble()
		{
			ConditionalBranchTest(OpCodes.Ble_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_S, 2, 5, true);
		}

		[Test]
		public void Test_Blt()
		{
			ConditionalBranchTest(OpCodes.Blt_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_S, 2, 5, true);
		}

		[Test]
		public void Test_Bge_Un()
		{
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Bge_Un_S, 2, 5, false);
		}

		[Test]
		public void Test_Bgt_Un()
		{
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 2, true);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Bgt_Un_S, 2, 5, false);
		}

		[Test]
		public void Test_Ble_Un()
		{
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 5, 5, true);
			ConditionalBranchTest(OpCodes.Ble_Un_S, 2, 5, true);
		}

		[Test]
		public void Test_Blt_Un()
		{
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 2, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 5, 5, false);
			ConditionalBranchTest(OpCodes.Blt_Un_S, 2, 5, true);
		}

		[Test]
		public void Test_Add_I4()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ldc_I4_3);
			w.WriteOpcode(OpCodes.Add);
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, 10);
		}

		[Test]
		public void Test_Sub_I4()
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ldc_I4_3);
			w.WriteOpcode(OpCodes.Sub);
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, 4);
		}

		[Test]
		public void Test_Mul_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Mul, 5, 3, 15);
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
			ExecuteAndVerifyBinaryOperator(OpCodes.Mul, 3.5f, 4f, 3.5f * 7.5f);
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
			ExecuteAndVerifyBinaryOperator(OpCodes.Ceq, 5, 5, 1);
		}

		[Test]
		public void Test_Cgt_I4()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 3, 1);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 5, 0);
			ExecuteAndVerifyBinaryOperator(OpCodes.Cgt, 5, 7, 0);
		}

		[Test, Ignore("Enable when division works.")]
		public void Test_Div_Un()
		{
			ExecuteAndVerifyBinaryOperator(OpCodes.Div_Un, 16, 4, 4);
//			ExecuteAndVerifyBinaryOperator(OpCodes.Div_Un, 16, 5, 3);
//			ExecuteAndVerifyBinaryOperator(OpCodes.Div_Un, 2, 7, 0);
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

			cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);
			Disassembler.DisassembleUnconditionalToConsole(cc);

			cc.PerformProcessing(CompileContextState.S8Complete);

//			cc.WriteAssemblyToFile(Utilities.GetUnitTestName()+"_asm.s");

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

		public void ExecuteAndVerifyBinaryOperator(OpCode opcode, int i1, int i2, int expectedValue)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, expectedValue);
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

			Execution(w, expectedValue);
		}


		private static void Execution<T>(ILWriter ilcode, T expectedValue) where T : struct
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
											  delegate { return nextspillOffset++; });
				
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

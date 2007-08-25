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
			InstTest(OpCodes.Mul, 5, 3, 15);
		}

		[Test]
		public void Test_And()
		{
			InstTest(OpCodes.And, 0x0f0, 0xf00, 0x000);
			InstTest(OpCodes.And, 0x0ff, 0xff0, 0x0f0);
		}

		[Test]
		public void Test_Or()
		{
			InstTest(OpCodes.Or, 0xf00, 0x0f0, 0xff0);
		}

		[Test]
		public void Test_Xor()
		{
			InstTest(OpCodes.Xor, 0xff0, 0x0f0, 0xf00);
		}
		
		[Test]
		public void Test_Ceq_I4()
		{
			InstTest(OpCodes.Ceq, 5, 3, 0);
			InstTest(OpCodes.Ceq, 5, 5, 1);
		}

		[Test]
		public void Test_Cgt_I4()
		{
			InstTest(OpCodes.Cgt, 5, 3, 1);
			InstTest(OpCodes.Cgt, 5, 5, 0);
			InstTest(OpCodes.Cgt, 5, 7, 0);
		}

		[Test]
		public void Test_Div_Un()
		{
			InstTest(OpCodes.Div_Un, 16, 4, 4);
//			InstTest(OpCodes.Div_Un, 16, 5, 3);
//			InstTest(OpCodes.Div_Un, 2, 7, 0);
		}

		public void InstTest(OpCode opcode, int i1, int i2, int exp)
		{
			ILWriter w = new ILWriter();

			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i1);
			w.WriteOpcode(OpCodes.Ldc_I4);
			w.WriteInt32(i2);
			w.WriteOpcode(opcode);
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, exp);
		}


		private static void Execution<T>(ILWriter ilcode, T expetedValue) where T : struct
		{
			RegisterSizedObject returnAddressObject = new RegisterSizedObject();
			returnAddressObject.Offset = Utilities.Align16(0x1000);

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

			new TreeDrawer().DrawMethod(basicBlocks);

			RecursiveInstructionSelector sel = new RecursiveInstructionSelector();

			ReadOnlyCollection<MethodParameter> par = new ReadOnlyCollection<MethodParameter>(new List<MethodParameter>());

			SpuManualRoutine spum = new SpuManualRoutine(false);

			spum.Writer.BeginNewBasicBlock();
			SpuAbiUtilities.WriteProlog(2, spum.Writer);

			sel.GenerateCode(basicBlocks, par, spum.Writer);

			Console.WriteLine(spum.Writer.Disassemble());

//			new Disassembler().Disassemble(new ObjectWithAddress[] {spum}, Console.Out);

			// TODO Det håndteres muligvis ikke virtuelle moves i SimpleRegAlloc.
			new SimpleRegAlloc().alloc(spum.Writer.BasicBlocks);

//			new Disassembler().Disassemble(new ObjectWithAddress[] { spum }, Console.Out);
			Console.WriteLine(spum.Writer.Disassemble());

			RegAllocGraphColloring.RemoveRedundantMoves(spum.Writer.BasicBlocks);

			Console.WriteLine(spum.Writer.Disassemble());

			spum.Offset = 1024;
			// This also creates an epilog.
			spum.PerformAddressPatching();

			SpuInitializer spuinit = new SpuInitializer(spum, returnAddressObject);
			spuinit.Offset = 0;
			spuinit.PerformAddressPatching();

			int[] code = new int[1024];
			CompileContext.CopyCode(code, new SpuRoutine[] { spuinit, spum });

			Disassembler.DisassembleToConsole(new ObjectWithAddress[] { spuinit, spum, returnAddressObject });

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();

				T returnValue = ctx.DmaGetValue<T>((LocalStorageAddress) returnAddressObject.Offset);

				AreEqual(expetedValue, returnValue, "SPU delegate execution returned a wrong value.");
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

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

				throw;
			}

			RecursiveInstructionSelector sel = new RecursiveInstructionSelector();

			ReadOnlyCollection<MethodParameter> par = new ReadOnlyCollection<MethodParameter>(new List<MethodParameter>());

			SpuManualRoutine spum = new SpuManualRoutine(false);

			spum.Writer.BeginNewBasicBlock();
			SpuAbiUtilities.WriteProlog(2, spum.Writer);

			sel.GenerateCode(basicBlocks, par, spum.Writer);

			// TODO Det håndteres muligvis ikke virtuelle moves i SimpleRegAlloc.
			new SimpleRegAlloc().alloc(spum.Writer.BasicBlocks);

			RegAllocGraphColloring.RemoveRedundantMoves(spum.Writer.BasicBlocks);

			spum.Offset = 1024;
			spum.PerformAddressPatching();

			SpuInitializer spuinit = new SpuInitializer(spum, returnAddressObject);
			spuinit.Offset = 0;
			spuinit.PerformAddressPatching();

			int[] initcode = spuinit.Emit();
			int[] methodcode = spum.Emit();

			Assert.Less(initcode.Length * 4, 1025, "SpuInitializer code is to large", null);

			int[] code = new int[1024/4 + methodcode.Length];

			Buffer.BlockCopy(initcode, 0, code, 0, initcode.Length*4);
			Buffer.BlockCopy(methodcode, 0, code, 1024, methodcode.Length*4);

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
	}
}

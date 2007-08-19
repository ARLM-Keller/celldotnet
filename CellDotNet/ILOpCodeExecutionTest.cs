using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Emit;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ILOpCodeExecutionTest : UnitTest
	{

		private delegate T Getter<T>();

		[Test]
		public void Test()
		{
			
		}


		[Test]
		public void Test_Ret()
		{
//			Getter<int> g = delegate { return 500; };
//			CompareExecution(g);

			ILWriter w = new ILWriter();
			w.WriteOpcode(OpCodes.Ldc_I4_7);
			w.WriteOpcode(OpCodes.Ret);

			Execution(w, 7);

			//			w.WriteOpcode(OpCodes.Ldc_I4_4);
			//			w.WriteOpcode(OpCodes.Br_S);
			//			w.WriteByte(1);
			//			w.WriteOpcode(OpCodes.Ldc_I4_7);
			//			w.WriteOpcode(OpCodes.Pop);


		}


		private static void Execution<T>(ILWriter ilcode, T expetedValue) where T : struct
		{
			IRTreeBuilder builder = new IRTreeBuilder();
			List<MethodVariable> vars = new List<MethodVariable>();
			List<IRBasicBlock> basicBlocks = builder.BuildBasicBlocks(ilcode.CreateReader(), vars);

			RecursiveInstructionSelector sel = new RecursiveInstructionSelector();

			ReadOnlyCollection<MethodParameter> par = new ReadOnlyCollection<MethodParameter>(new List<MethodParameter>());

			SpuManualRoutine spum = new SpuManualRoutine(false);

			sel.GenerateCode(basicBlocks, par, spum.Writer);

			// TODO Det håndteres muligvis ikke virtuelle moves i SimpleRegAlloc.
			new SimpleRegAlloc().alloc(spum.Writer.BasicBlocks);

			RegAllocGraphColloring.RemoveRedundantMoves(spum.Writer.BasicBlocks);

			spum.Offset = 1024;
			spum.PerformAddressPatching();

			RegisterSizedObject returnAddressObject = new RegisterSizedObject();
			returnAddressObject.Offset = Utilities.Align16(spum.Size+spum.Offset);

			SpuInitializer spuinit = new SpuInitializer(spum, returnAddressObject);
			spuinit.Offset = 0;
			spuinit.PerformAddressPatching();

			Console.WriteLine("Disasemble spuinit:");
			Console.WriteLine(spuinit._writer.Disassemble());

			Console.WriteLine("Disasemble spumethod:");
			Console.WriteLine(spum.Writer.Disassemble());

			int[] initcode = spuinit.Emit();
			int[] methodcode = spum.Emit();

			Assert.Less(initcode.Length * 4, 1025, "SpuInitializer code is to large", null);

			int[] code = new int[1024/4 + methodcode.Length];

			Buffer.BlockCopy(initcode, 0, code, 0, initcode.Length);
			Buffer.BlockCopy(methodcode, 0, code, 1024/4, methodcode.Length);

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

		private static void CompareExecution<T>(Getter<T> getter) where T : IComparable<T>
		{
			CompileContext cc = new CompileContext(getter.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();
			}

			// TODO: Run both delegates and compare the return value.
			throw new NotImplementedException();

//			T t1 = getter();
//			T t2 = default(T);
//
//			AreEqual(t1, t2, "SPU delegate execution returned a different value.");
		}



	}
}

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpeContextTest : UnitTest
	{
		[Test]
		public void SpeSimpleDMATest()
		{
			if (!SpeContext.HasSpeHardware)
				return;
			using (SpeContext ctxt = new SpeContext())
			{
				ctxt.LoadProgram(new int[] { 13 });

				int[] lsa = ctxt.GetCopyOffLocalStorage();

				if (lsa[0] != 13)
					Assert.Fail("DMA error.");
			}
		}

		private delegate void BasicTestDelegate();

		[Test]
		unsafe public void TestFirstCellProgram()
		{
			BasicTestDelegate del = delegate
										{
											int* i;
											i = (int*)0x40;
											*i = 34;
										};


			MethodBase method = del.Method;
			MethodCompiler mc = new MethodCompiler(method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(mc);


			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
			mc.GetBodyWriter().WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			Console.WriteLine();
			Console.WriteLine("Disassembly after regalloc: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S6RemoveRedundantMoves);

			Console.WriteLine();
			Console.WriteLine("Disassembly after remove of redundant moves: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			int[] bincode = SpuInstruction.emit(mc.GetBodyWriter().GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(bincode);

				ctx.Run();
				int[] ls = ctx.GetCopyOffLocalStorage();

				if (ls[0x40 / 4] != 34)
				{
					Console.WriteLine("øv");
					Console.WriteLine("Value: {0}", ls[0x40/4]);
				}
			}
		}

		[Test]
		public void TestPutGetInt32()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress) 32, 33000);
				ctx.DmaPutValue((LocalStorageAddress) 64, 34000);

				int readvalue = ctx.DmaGetValue<int>((LocalStorageAddress) 32);
				AreEqual(33000, readvalue);
			}
		}

		[Test]
		public void TestPutGetFloat()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress)32, 33000f);
				ctx.DmaPutValue((LocalStorageAddress)64, 34000f);

				float readvalue = ctx.DmaGetValue<float>((LocalStorageAddress)32);
				AreEqual(33000f, readvalue);
			}
		}


		[Test]
		public void TestHasSpe()
		{
			// For this test we assume that a windows machine does not have spe hw,
			// and that anything else has spe hw.
			if (Path.DirectorySeparatorChar == '\\')
				IsFalse(SpeContext.HasSpeHardware);
			else
				IsTrue(SpeContext.HasSpeHardware);
		}

		[Test, Explicit]
		public void TestGetSpeControlArea()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext spe = new SpeContext())
			{
				SpeControlArea area = spe.GetControlArea();
				AreEqual((uint)0, area.SPU_NPC);
			}
		}

		[Test]
		public void TestReturnInt32_Manual()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			Disassembler.DisassembleToConsole(cc);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				Console.WriteLine("Mem initially:");
				Utilities.DumpMemory(ctx, (LocalStorageAddress)0, 0x70, Console.Out);

				int[] code = cc.GetEmittedCode();
				Console.WriteLine("Code:");
				Utilities.DumpMemory(code, 0, (LocalStorageAddress)0, code.Length * 4, Console.Out);
				ctx.LoadProgram(code);
				int rc = ctx.Run();

				Console.WriteLine("Memory after execution:");
				Utilities.DumpMemory(ctx, (LocalStorageAddress) 0, 0x80, Console.Out);
				Console.WriteLine();

				AreEqual(0, rc);

				int retval = ctx.DmaGetValue<int>(cc.ReturnValueAddress);
				AreEqual(magicNumber, retval);
			}
		}

		[Test]
		public void TestReturnInt32()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object retval = ctx.RunProgram(del);
				AreEqual(typeof(int), retval.GetType());
				AreEqual(magicNumber, (int)retval);
			}
		}

		private delegate int IntReturnDelegate();

		[Test, Ignore("Enable when delegate wrapper is supported.")]
		public void TestDelegateRun_IntReturn()
		{
			IntReturnDelegate del = delegate { return 40; };
			IntReturnDelegate del2 = CompileContext.CreateSpeDelegate(del);

			int retval = del2();
			AreEqual(40, retval);
		}
	}
}

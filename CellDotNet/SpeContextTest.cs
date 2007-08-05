using System;
using System.Collections.Generic;
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
			SpeContext ctxt = new SpeContext();
			ctxt.LoadProgram(new int[] { 13 });

			int[] lsa = ctxt.GetCopyOffLocalStorage();

			if (lsa[0] != 13)
				Assert.Fail("DMA error.");
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

			new TreeDrawer().DrawMethod(mc, method);

			mc.PerformProcessing(MethodCompileState.S6PrologAndEpilogDone);
			mc.GetBodyWriter().WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			int[] bincode = SpuInstruction.emit(mc.GetBodyWriter().GetAsList());

			SpeContext ctx = new SpeContext();
			ctx.LoadProgram(bincode);

			ctx.Run();
			int[] ls = ctx.GetCopyOffLocalStorage();

			if (ls[0x40 / 4] != 34)
			{
				Console.WriteLine("øv");
				Console.WriteLine("Value: {0}", ls[0xff0 / 4]);
			}
			else
				Console.WriteLine("Selvfølgelig :)");
		}

		[Test]
		public void TestGetPutInt32()
		{
			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPut(32, 33000);
				ctx.DmaPut(36, 34000);

				int readvalue = ctx.DmaGetInt32(32);
				AreEqual(33000, readvalue);
			}
		}
	}

	[TestFixture]
	public class Align16Test : UnitTest
	{
		[Test]
		unsafe public void TestAlignment16()
		{
			SpeContext.Align16 a = new SpeContext.Align16(), b = new SpeContext.Align16();
			long pa = (long)a.Get16BytesAlignedAddress();
			long pb = (long)b.Get16BytesAlignedAddress();

			AreEqual(0L, pa & 0xf, "Bad alignment for a.");
			AreEqual(0L, pb & 0xf, "Bad alignment for b.");
		}

		[Test]
		public unsafe void TestAlignment8()
		{
			SpeContext.Align16 a = new SpeContext.Align16(), b = new SpeContext.Align16();
			long pa = (long)a.Get8BytesAlignedAddress();
			long pb = (long)b.Get8BytesAlignedAddress();

			AreEqual(0L, pa & 7, "Bad alignment for a.");
			AreEqual(0L, pb & 7, "Bad alignment for b.");
		}

	}

}

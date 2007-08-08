using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
				Console.WriteLine("�v");
				Console.WriteLine("Value: {0}", ls[0xff0 / 4]);
			}
			else
				Console.WriteLine("Selvf�lgelig :)");
		}

		[Test]
		public void TestGetPutInt32()
		{
			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPut((LocalStorageAddress) 32, 33000);
				ctx.DmaPut((LocalStorageAddress) 64, 34000);

				int readvalue = ctx.DmaGetInt32((LocalStorageAddress) 32);
				AreEqual(33000, readvalue);
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpeContextTest
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
			MethodCompiler ci = new MethodCompiler(method);
			ci.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			new TreeDrawer().DrawMethod(ci, method);

			ILTreeSpuWriter writer = new ILTreeSpuWriter();
			SpuInstructionWriter ilist = new SpuInstructionWriter();
			writer.GenerateCode(ci, ilist);
			ilist.WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(ilist.Disassemble());


			SimpleRegAlloc regalloc = new SimpleRegAlloc();
			List<SpuInstruction> asm = new List<SpuInstruction>(ilist.Instructions);
			regalloc.alloc(asm, 16);

			int[] bincode = SpuInstruction.emit(asm);

			SpeContext ctx = new SpeContext();
			if (!ctx.LoadProgram(bincode))
			{
				Console.WriteLine("Program load failed!");
				return;
			}

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
	}
}

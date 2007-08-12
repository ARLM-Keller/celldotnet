using System;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class RegAllocGraphColloringTest : UnitTest
	{
		//TODO work out more test cases.

		private delegate void BasicTestDelegate();

		[Test]
		public unsafe void SimpleTest()
		{
			BasicTestDelegate del = delegate
							{
								int* i;
								i = (int*)0x40;
								*i = 34;
							};


			MethodBase method = del.Method;
			MethodCompiler mc = new MethodCompiler(method);

			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);

			new TreeDrawer().DrawMethod(mc);

			mc.GetBodyWriter().WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			Console.WriteLine();
			Console.WriteLine("Disassembly after regalloc: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

		}

	}
}

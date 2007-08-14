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

		[Test]
		public unsafe void LargeTest()
		{
			BasicTestDelegate del = delegate
							{
								int* i;
								int* j;
								int* k;
								int* l;

								i = (int*) 0x40;
								j = (int*) 0x44;
								k = (int*) 0x48;
								l = (int*) 0x4c;

								*i = 34;

								int s = 0;

								int a = *i;

								s++;

								int b = a*42;

								s++;

								int c = b/7;

								s++;

								*j = b;
								*k = c;

								s++;

								*l = s;
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
		}
	}
}

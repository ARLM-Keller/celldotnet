// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if UNITTEST

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

		[Test, Ignore("Pointers are no longer supported.")]
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

			mc.GetBodyWriter().WriteStop();

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);
		}

		[Test, Ignore("Pointers are no longer supported.")]
		public unsafe void LargeTest()
		{
			BasicTestDelegate del = delegate
							{
								int* i;
								int* j;
								int* k;
								int* l;

								i = (int*) 0x40;
								j = (int*) 0x50;
								k = (int*) 0x60;
								l = (int*) 0x70;

								*i = 34;

								int s = 0;

								int a = *i;

								s++;

								int b = a*42;

								s++;

								int c = b+7;

								s++;

								*j = b;
								*k = c;

								s++;

								*l = s;
							};


			MethodBase method = del.Method;
			MethodCompiler mc = new MethodCompiler(method);

			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

//			new TreeDrawer().DrawMethod(mc);

			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);

			mc.GetBodyWriter().WriteStop();

//			Console.WriteLine();
//			Console.WriteLine("Disassembly: ");
//			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			long t1 = DateTime.Now.Ticks;

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			long t2 = DateTime.Now.Ticks;
			
//			Console.WriteLine();
//			Console.WriteLine("Disassembly after regalloc: ");
//			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			long t3 = DateTime.Now.Ticks;

			RegAllocGraphColloring.RemoveRedundantMoves(mc.SpuBasicBlocks);

			long t4 = DateTime.Now.Ticks;

//			Console.WriteLine();
//			Console.WriteLine("Disassembly after remove of redundant moves: ");
//			Console.WriteLine(mc.GetBodyWriter().Disassemble());
//
//			Console.WriteLine("Reg alloc time: {0} Remove redundante moves: {1}", (t2-t1)/10000, (t4-t3)/10000);
		}
	}
}
#endif
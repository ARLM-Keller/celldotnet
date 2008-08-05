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
using System.Collections.Generic;
using CellDotNet.Intermediate;
using NUnit.Framework;



namespace CellDotNet.Spe
{
	[TestFixture]
	public class MethodCompilerTest : UnitTest
	{
		public void TestBranchCodeGenerationBasic()
		{
			Action del = delegate
				{
					int i = 0;

					for (int j = 0; j < 10; j++)
					{
						if (i < 4)
							i = 4;
						else
							i = 10;
					}
				};


			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S8AddressPatchingDone);

			mc.GetPrologWriter().AssertNoPseudoInstructions();
			mc.GetBodyWriter().AssertNoPseudoInstructions();
			mc.GetEpilogWriter().AssertNoPseudoInstructions();
		}

		private delegate void FiveIntegerArgumentDelegate(int i1, int i2, int i3, int i4, int i5);


		/// <summary>
		/// Usesd solely for making variables/arguments escape.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="arg"></param>
		static void DummyByrefFunction<T>(ref T arg)
		{
			// Nothing.
		}

		[Test]
		public void TestFrameArgumentEscapeDetection()
		{
			FiveIntegerArgumentDelegate del = delegate(int i1, int i2, int i3, int i4, int i5)
				{
					int li1 = Math.Max(234, i3);
					int li2 = Math.Max(li1, i4);

					// Arguments i1, i2 and i5 should escape, and so should li2.
					DummyByrefFunction(ref i1);
					DummyByrefFunction(ref i2);
					DummyByrefFunction(ref i5);
					DummyByrefFunction(ref li2);
				};

			MethodCompiler mc = new MethodCompiler(del.Method);
			mc.PerformProcessing(MethodCompileState.S3InstructionSelectionPreparationsDone);

			// Find names of escaping locals and variables.
			List<string> paramnamelist = new List<string>();
			foreach (MethodParameter p in mc.Parameters)
			{
				if (p.Escapes.Value)
					paramnamelist.Add(p.Name);
			}
			if (!Algorithms.AreEqualSets(paramnamelist, new string[] {"i1", "i2", "i5"}, StringComparer.Ordinal))
				Assert.Fail("Didn't correctly determine escaping parameters.");

			List<int> varindices = new List<int>();
			foreach (MethodVariable v in mc.Variables)
			{
				if (v.Escapes.Value)
					varindices.Add(v.Index);
			}
			if (varindices.Count != 1 || varindices[0] != 1)
				Assert.Fail("Didn't correctly determine escaping varaible.");
		}

		[Test]
		public void Test()
		{
#if !DEBUG 
			Assert.Fail("No debug mode.");
#endif
		}
	}
}
#endif
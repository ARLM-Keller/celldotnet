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
using NUnit.Framework;


namespace CellDotNet.Spe
{
	[TestFixture]
	public class CompileContextTest : UnitTest
	{
		private static void MethodCallerInternal()
		{
			MethodCalleeInternal();
		}

		private static void MethodCalleeInternal()
		{
		}

		private static void MethodCallerExternal()
		{
			MethodCalleeInternal();
			Math.Max(4, 99);
		}

		private static void MethodRecursiveCaller()
		{
			MethodRecursiveCaller();
		}

		[Test]
		public void TestAcquireTwoMethodsInternal()
		{
			Action del = MethodCallerInternal;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(2, cc.Methods.Count);
		}

		[Test]
		public void TestAcquireThreeMethodsExternal()
		{
			Action del = MethodCallerExternal;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(3, cc.Methods.Count);
		}

		[Test]
		public void TestAcquireRecursiveMethod()
		{
			Action del = MethodRecursiveCaller;
			
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);

			AreEqual(1, cc.Methods.Count);
			
		}

		[Test]
		public void TestEmptyMethod()
		{
			Action del = delegate { };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
		}
	}
}
#endif
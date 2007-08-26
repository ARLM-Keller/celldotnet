using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
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

		delegate void SimpleDelegate();

		[Test]
		public void TestAcquireTwoMethodsInternal()
		{
			SimpleDelegate del = MethodCallerInternal;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(2, cc.Methods.Count);
		}

		[Test]
		public void TestAcquireThreeMethodsExternal()
		{
			SimpleDelegate del = MethodCallerExternal;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(3, cc.Methods.Count);
		}

		[Test]
		public void TestAcquireRecursiveMethod()
		{
			SimpleDelegate del = MethodRecursiveCaller;
			
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);

			AreEqual(1, cc.Methods.Count);
			
		}

		[Test]
		public void TestEmptyMethod()
		{
			SimpleDelegate del = delegate { };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class CompileContextTest
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

		delegate void SimpleDelegate();

		[Test]
		public void TestAcquireTwoMethodsInternal()
		{
			SimpleDelegate del = MethodCallerInternal;
			MethodBase method = del.Method;

			CompileContext cc = new CompileContext(method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(2, cc.Methods.Count);
		}

		[Test]
		public void TestAcquireThreeMethodsExternal()
		{
			SimpleDelegate del = MethodCallerExternal;
			MethodBase method = del.Method;

			CompileContext cc = new CompileContext(method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			Assert.AreEqual(3, cc.Methods.Count);

			
		}

		[Test]
		unsafe public void TestRunStaticMethodCall()
		{
			SimpleDelegate del = delegate
							{
								int* i;
								i = (int*)30000;
								*i = Math.Max(100, 200);
							};
			MethodBase method = del.Method;
			CompileContext cc = new CompileContext(method);
			cc.PerformProcessing(CompileContextState.S7Complete);
		}
	}
}

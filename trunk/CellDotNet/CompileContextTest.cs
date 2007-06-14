using System;
using System.Collections.Generic;
using Mono.Cecil;
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
		public void TestCompileTwoMethodsInternal()
		{
			SimpleDelegate del = MethodCallerInternal;
			MethodDefinition method = CompileInfoTest.GetMethod(del);

			CompileContext cc = new CompileContext(method);
			Assert.AreEqual(2, cc.Methods.Count);
		}

		[Test]
		public void TestCompileThreeMethodsExternal()
		{
			SimpleDelegate del = MethodCallerExternal;
			MethodDefinition method = CompileInfoTest.GetMethod(del);

			CompileContext cc = new CompileContext(method);
			Assert.AreEqual(3, cc.Methods.Count);
		}
	}
}

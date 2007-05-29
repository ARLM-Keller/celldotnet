using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class CompileInfoTest
	{
		private delegate void BasicTestDelegate();

		[Test]
		public void TestParseFunctionCall()
		{
			BasicTestDelegate del = delegate
										{
											int rem;
											Math.DivRem(9, 13, out rem);
											Math.Max(Math.Min(3, 1), 5L);
										};
			MethodDefinition method = Class1.GetMethod(del);
			CompileInfo ci = new CompileInfo(method);
			new TreeDrawer().DrawMethod(ci, method);
		}
	}
}

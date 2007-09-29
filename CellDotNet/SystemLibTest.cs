using System;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SystemLibTest : UnitTest
	{
		private delegate int IntDelegate();


		[Test]
		public void TestSystemLib_Math_Abs()
		{
			Converter<int, int> del = delegate(int input)
			                          	{
			                          		 return System.Math.Abs(input);
			                          	};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int arg = -17;

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(del(arg), (int)SpeContext.UnitTestRunProgram(cc, arg));
		}
	}
}

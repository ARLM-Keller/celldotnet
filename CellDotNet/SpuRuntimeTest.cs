using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuRunTimeTest : UnitTest
	{
//		private delegate void SimpleDelegate();
		private delegate int IntDelegate();

		[Test]
		public void TestStop()
		{
			const int magicnumber = 45;
			IntDelegate del = 
				delegate
					{
						SpuRuntime.Stop();
						return magicnumber;
					};
			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);
			// Stop is an intrinsic.
			AreEqual(1, cc.Methods.Count);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc);
				// The return value shouldn't have made it to $3.
				AreEqual(0, (int) rv);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MfcTest : UnitTest
	{
		private delegate int IntDelegate();

		[Test]
		public void TestGetQueueDepth()
		{
			IntDelegate del = delegate { return Mfc.GetAvailableQueueEntries(); };
			IntDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			SpeDelegateRunner runner = (SpeDelegateRunner) del2.Target;
			AreEqual(1, runner.CompileContext.Methods.Count);

			if (!SpeContext.HasSpeHardware)
				return;

			int depth = del2();
			AreEqual(16, depth);
		}
	}
}

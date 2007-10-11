using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuOpCodeTest : UnitTest
	{
		[Test]
		public void TestChannelOpCode()
		{
			IsFalse(SpuOpCode.rchcnt.RegisterRtNotWritten);
			IsTrue(SpuOpCode.rchcnt.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rchcnt.Parts);

			IsFalse(SpuOpCode.rdch.RegisterRtNotWritten);
			IsTrue(SpuOpCode.rdch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rdch.Parts);

			IsTrue(SpuOpCode.wrch.RegisterRtNotWritten);
			IsTrue(SpuOpCode.wrch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.wrch.Parts);
		}

		[Test]
		public void TestLoad()
		{
			IsFalse(SpuOpCode.lqd.RegisterRtNotWritten);
			IsTrue(SpuOpCode.lqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.lqd.Parts);
		}

		[Test]
		public void TestStore()
		{
			IsTrue(SpuOpCode.stqd.RegisterRtNotWritten);
			IsTrue(SpuOpCode.stqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.stqd.Parts);
		}
	}
}

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuOpCodeTest : UnitTest
	{
		[Test]
		public void TestChannel()
		{
			IsTrue(SpuOpCode.rchcnt.NoRegisterWrite);
			IsTrue(SpuOpCode.rchcnt.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rchcnt.Parts);

			IsTrue(SpuOpCode.rdch.NoRegisterWrite);
			IsTrue(SpuOpCode.rdch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.rdch.Parts);

			IsFalse(SpuOpCode.wrch.NoRegisterWrite);
			IsTrue(SpuOpCode.wrch.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ca, SpuOpCode.wrch.Parts);
		}

		[Test]
		public void TestLoad()
		{
			IsFalse(SpuOpCode.lqd.NoRegisterWrite);
			IsTrue(SpuOpCode.lqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.lqd.Parts);
		}

		[Test]
		public void TestStore()
		{
			IsTrue(SpuOpCode.stqd.NoRegisterWrite);
			IsTrue(SpuOpCode.stqd.HasImmediate);
			AreEqual(SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate, SpuOpCode.stqd.Parts);
		}
	}
}

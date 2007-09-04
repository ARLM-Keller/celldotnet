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
			AreEqual(SpuOpCodeRegisterUsage.Rt | SpuOpCodeRegisterUsage.Ca, SpuOpCode.rchcnt.RegisterUsage);

			IsTrue(SpuOpCode.rdch.NoRegisterWrite);
			IsTrue(SpuOpCode.rdch.HasImmediate);
			AreEqual(SpuOpCodeRegisterUsage.Rt | SpuOpCodeRegisterUsage.Ca, SpuOpCode.rdch.RegisterUsage);

			IsFalse(SpuOpCode.wrch.NoRegisterWrite);
			IsTrue(SpuOpCode.wrch.HasImmediate);
			AreEqual(SpuOpCodeRegisterUsage.Rt | SpuOpCodeRegisterUsage.Ca, SpuOpCode.wrch.RegisterUsage);
		}

		[Test]
		public void TestLoad()
		{
			IsFalse(SpuOpCode.lqd.NoRegisterWrite);
			IsTrue(SpuOpCode.lqd.HasImmediate);
			AreEqual(SpuOpCodeRegisterUsage.Rt | SpuOpCodeRegisterUsage.Ra, SpuOpCode.lqd.RegisterUsage);
		}

		[Test]
		public void TestStore()
		{
			IsTrue(SpuOpCode.stqd.NoRegisterWrite);
			IsTrue(SpuOpCode.stqd.HasImmediate);
			AreEqual(SpuOpCodeRegisterUsage.Rt | SpuOpCodeRegisterUsage.Ra, SpuOpCode.stqd.RegisterUsage);
		}
	}
}

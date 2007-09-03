using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class TypeDeriverTest : UnitTest
	{
		[Test]
		public void TestNumeric_I4_I4()
		{
			StackTypeDescription rv = TypeDeriver.GetNumericResultType(StackTypeDescription.Int32, StackTypeDescription.Int32);
			AreEqual(StackTypeDescription.Int32, rv);
		}

		[Test]
		public void TestNumeric_NativeInt_I4()
		{
			StackTypeDescription rv = TypeDeriver.GetNumericResultType(StackTypeDescription.Int32.GetPointer(), StackTypeDescription.Int32);
			AreEqual(StackTypeDescription.Int32.GetPointer(), rv);
		}
	}
}

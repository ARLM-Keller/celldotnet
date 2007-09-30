using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MarshalerTest : UnitTest
	{
		[Test]
		public void TestSimpleTypes()
		{
			object[] arr = new object[] { 1, 3f, 4d, (short)5 };
			byte[] buf = new Marshaler().GetArguments(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(int), typeof(float), typeof(double), typeof(short) });
			AreEqual(arr, arr2);
		}
	}
}

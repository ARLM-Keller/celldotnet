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

		[Test]
		public void TestVectorTypes()
		{
			object[] arr = new object[] { new Int32Vector(1, 2, 3, 4), new Float32Vector(1, 2, 3, 4) };
			byte[] buf = new Marshaler().GetArguments(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(Int32Vector), typeof(Float32Vector) });
			AreEqual(arr, arr2);
		}

		[Test]
		public void TestOtherStructs()
		{
			object[] arr = new object[] { new MainStorageArea((IntPtr) 0x12323525), (IntPtr) 0x34985221 };
			byte[] buf = new Marshaler().GetArguments(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(MainStorageArea), typeof(IntPtr) });
			AreEqual(arr, arr2);
		}
	}
}

// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if UNITTEST

using System;
using System.Collections.Generic;
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
			byte[] buf = new Marshaler().GetImage(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(int), typeof(float), typeof(double), typeof(short) });
			AreEqual(arr, arr2);
		}

		[Test]
		public void TestVectorTypes()
		{
			object[] arr = new object[] { new Int32Vector(1, 2, 3, 4), new Float32Vector(1, 2, 3, 4) };
			byte[] buf = new Marshaler().GetImage(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(Int32Vector), typeof(Float32Vector) });
			AreEqual(arr, arr2);
		}

		[Test]
		public void TestOtherStructs()
		{
			object[] arr = new object[] { new MainStorageArea((IntPtr) 0x12323525), (IntPtr) 0x34985221 };
			byte[] buf = new Marshaler().GetImage(arr);

			AreEqual(arr.Length * 16, buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(MainStorageArea), typeof(IntPtr) });
			AreEqual(arr, arr2);
		}

		struct TestBigStruct_Struct
		{
			private int i1;
			private int i2;
			private int i3;
			private int i4;
			private float f1;
			private float f2;
			private float f3;
			private float f4;

			public TestBigStruct_Struct(int i1, int i2, int i3, int i4, float f1, float f2, float f3, float f4)
			{
				this.i1 = i1;
				this.i2 = i2;
				this.i3 = i3;
				this.i4 = i4;
				this.f1 = f1;
				this.f2 = f2;
				this.f3 = f3;
				this.f4 = f4;
			}
		}

		[Test]
		public void TestBigStruct()
		{
			object[] arr = new object[] { new TestBigStruct_Struct(1, 2, 0x34985221, 4, 5, 6, 7, 8), 0x34985221 };
			byte[] buf = new Marshaler().GetImage(arr);

			IsTrue(arr.Length * 16 <= buf.Length);
			object[] arr2 = new Marshaler().GetValues(buf, new Type[] { typeof(TestBigStruct_Struct), typeof(int) });
			AreEqual(arr, arr2);
		}

		class MyRefType1
		{
		}

		class MyRefType2
		{
		}

		[Test]
		public void TestReferenceTypes()
		{
			object[] arr = new object[] { new MyRefType1(), new MyRefType2() };
			Marshaler m = new Marshaler();
			byte[] buf = m.GetImage(arr);

			AreEqual(32, buf.Length);
			object[] arr2 = m.GetValues(buf, new Type[] { typeof(MyRefType1), typeof(MyRefType2) });
			AreEqual(arr, arr2);
		}
	}
}

#endif
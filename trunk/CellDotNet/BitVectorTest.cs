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
using NUnit.Framework;


namespace CellDotNet
{
	[TestFixture]
	public class BitVectorTest : UnitTest
	{
		[Test]
		public void TestBitVector_AddElement()
		{
			BitVector b = new BitVector();
			b.Add(7);
			b.Add(9);
			b.Add(333);

			IsFalse(b.IsCountZero() || b.Count != 3 || !b.Contains(7) || !b.Contains(9) || !b.Contains(333));
		}

		[Test]
		public void TestBitVector_AddAllBitVector()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.AddAll(b1);

			IsFalse(b2.IsCountZero() || b2.Count != 3 || !b2.Contains(7) || !b2.Contains(9) || !b2.Contains(333));
		}

		[Test]
		public void TestBitVector_AddAllIEnumerable()
		{
			BitVector b1 = new BitVector();

			uint[] array = new uint[] { 7, 9, 333 };

			b1.AddAll(array);

			IsFalse(b1.IsCountZero() || b1.Count != 3 || !b1.Contains(7) || !b1.Contains(9) || !b1.Contains(333));
		}

		[Test]
		public void TestBitVector_Equals()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(9);
			b2.Add(333);

			IsFalse(b2.IsCountZero() || !b2.Equals(b1));
		}

		[Test]
		public void TestBitVector_Clear()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			b1.Clear();

			IsFalse(!b1.IsCountZero() || b1.Count != 0 || b1.Contains(7) || b1.Contains(9) || b1.Contains(333));
		}

		[Test]
		public void TestBitVector_RemoveAllIEnumerableInt()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			int[] array = new int[] { 7, 333 };
			b1.RemoveAll(array);

			IsFalse(b1.IsCountZero() || b1.Count != 1 || b1.Contains(7) || !b1.Contains(9) || b1.Contains(333));
		}

		[Test]
		public void TestBitVector_RemoveAllIEnumerableUint()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			uint[] array = new uint[] { 7, 333 };
			b1.RemoveAll(array);

			IsFalse(b1.IsCountZero() || b1.Count != 1 || b1.Contains(7) || !b1.Contains(9) || b1.Contains(333));
		}

		[Test]
		public void TestBitVector_RemoveAllBitVector()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(333);

			b1.RemoveAll(b2);

			IsFalse(b1.IsCountZero() || b1.Count != 1 || b1.Contains(7) || !b1.Contains(9) || b1.Contains(333));
		}

		[Test]
		public void TestBitVector_And()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(333);

			b1.And(b2);

			IsFalse(b1.IsCountZero() || b1.Count != 2 || !b1.Contains(7) || b1.Contains(9) || !b1.Contains(333));
		}

		[Test]
		public void TestBitVector_opAnd()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(333);

			BitVector b3 = b1 & b2;

			IsFalse(b3.IsCountZero() || b3.Count != 2 || !b3.Contains(7) || b3.Contains(9) || !b3.Contains(333));
		}

		[Test]
		public void TestBitVector_opOr()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(333);

			BitVector b3 = b1 | b2;

			IsFalse(b3.IsCountZero() || b3.Count != 3 || !b3.Contains(7) || !b3.Contains(9) || !b3.Contains(333));
		}

		[Test]
		public void TestBitVector_AddAllAnd()
		{
			BitVector b1 = new BitVector();
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(333);
			b2.Add(14);

			BitVector b3 = new BitVector();
			b3.Add(7);
			b3.Add(15);

			b3.AddAllAnd(b1, b2);

			IsFalse(b3.IsCountZero() || b3.Count != 3 || !b3.Contains(7) || b3.Contains(9) || !b3.Contains(333) || b3.Contains(14) || !b3.Contains(15));
		}

		[Test]
		public void TestBitVector_RemoveAllAnd()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(333);

			BitVector b2 = new BitVector();
			b2.Add(7);
			b2.Add(14);

			BitVector b3 = new BitVector();
			b3.Add(7);
			b3.Add(15);

			b3.RemoveAllAnd(b1, b2);

			IsFalse(b3.IsCountZero() || b3.Count != 1 || b3.Contains(7) || b3.Contains(9) || b3.Contains(333) || b3.Contains(14) || !b3.Contains(15));
		}

		[Test]
		public void TestBitVector_GetEnumerator()
		{
			BitVector b1 = new BitVector();
			b1.Add(7);
			b1.Add(9);
			b1.Add(333);

			BitVector b2 = new BitVector();

			foreach (int i in b1)
			{
				b2.Add(i);
			}

			IsFalse(b2.IsCountZero() || b2.Count != 3 || !b1.Equals(b2));
		}

		[Test]
		public void TestBitVector_GetItem()
		{
			BitVector init = new BitVector();
			init.Add(7);
			init.Add(9);
			init.Add(333);

			BitVector b1 = new BitVector(init);

			BitVector b2 = new BitVector();

			for (int i = 0; i < 3; i++)
			{
				int item = (int) b1.getItem();
				b1.Remove(item);
				b2.Add(item);
			}

			IsFalse(b2.IsCountZero() || b2.Count != 3 || !init.Equals(b2));
		}
	}
}
#endif
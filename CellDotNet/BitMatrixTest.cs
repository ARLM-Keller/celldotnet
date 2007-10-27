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
	public class BitMatrixTest : UnitTest
	{
		[Test]
		public void TestBitMatrix_Add()
		{
			BitMatrix m = new BitMatrix();
			m.add(1,4);
			m.add(3, 77);
			m.add(94, 77);

			IsFalse(!m.contains(1,4) || !m.contains(3,77) || !m.contains(94,77));
		}

		[Test]
		public void TestBitMatrix_Clear()
		{
			BitMatrix m = new BitMatrix();
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.Clear();

			IsFalse(m.contains(1, 4) || m.contains(3, 77) || m.contains(94, 77));
		}

		[Test]
		public void TestBitMatrix_Remove()
		{
			BitMatrix m = new BitMatrix();
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.remove(94,77);

			IsFalse(!m.contains(1, 4) || !m.contains(3, 77) || m.contains(94, 77));
		}

		[Test]
		public void TestBitMatrix_GetRow()
		{
			BitMatrix m = new BitMatrix();
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.add(123, 3);
			m.add(123, 44);
			m.add(123, 555);

			BitVector b = m.GetRow(123);

			IsFalse(b.IsCountZero() || b.Count != 3 || !b.Contains(3) || !b.Contains(44) || !b.Contains(555));
		}
	}
}
#endif
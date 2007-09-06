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
			BitMatrix m = new BitMatrix(0,0);
			m.add(1,4);
			m.add(3, 77);
			m.add(94, 77);

			if(!m.contains(1,4) || !m.contains(3,77) || !m.contains(94,77))
				throw new Exception("");
		}

		[Test]
		public void TestBitMatrix_Clear()
		{
			BitMatrix m = new BitMatrix(0, 0);
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.clear();

			if (m.contains(1, 4) || m.contains(3, 77) || m.contains(94, 77))
				throw new Exception("");
		}

		[Test]
		public void TestBitMatrix_Remove()
		{
			BitMatrix m = new BitMatrix(0, 0);
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.remove(94,77);

			if (!m.contains(1, 4) || !m.contains(3, 77) || m.contains(94, 77))
				throw new Exception("");
		}

		[Test]
		public void TestBitMatrix_GetRow()
		{
			BitMatrix m = new BitMatrix(0, 0);
			m.add(1, 4);
			m.add(3, 77);
			m.add(94, 77);

			m.add(123, 3);
			m.add(123, 44);
			m.add(123, 555);

			BitVector b = m.GetRow(123);

			if (b.IsCountZero() || b.Count != 3 || !b.Contains(3) || !b.Contains(44) || !b.Contains(555))
				throw new Exception("");
		}
	}
}

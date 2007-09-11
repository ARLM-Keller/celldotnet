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
			m.Add(1,4);
			m.Add(3, 77);
			m.Add(94, 77);

			IsTrue((m.Contains(1, 4) && m.Contains(3, 77)) && m.Contains(94, 77));
			IsTrue((m.Contains(4, 1) && m.Contains(77, 3)) && m.Contains(77, 94));
		}

		[Test]
		public void TestBitMatrix_Clear()
		{
			BitMatrix m = new BitMatrix(0, 0);
			m.Add(1, 4);
			m.Add(3, 77);
			m.Add(94, 77);

			m.Clear();

			IsTrue((!m.Contains(1, 4) && !m.Contains(3, 77)) && !m.Contains(94, 77));
			IsTrue((!m.Contains(4, 1) && !m.Contains(77, 3)) && !m.Contains(77, 94));
		}

		[Test]
		public void TestBitMatrix_Remove()
		{
			BitMatrix m = new BitMatrix(0, 0);
			m.Add(1, 4);
			m.Add(3, 77);
			m.Add(94, 77);

			m.Remove(94,77);

			IsTrue((m.Contains(1, 4) && m.Contains(3, 77)) && !m.Contains(94, 77));
			IsTrue((m.Contains(4, 1) && m.Contains(77, 3)) && !m.Contains(77, 94));
		}
	}
}

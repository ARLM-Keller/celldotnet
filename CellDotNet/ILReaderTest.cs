using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using NUnit.Framework;

namespace CellDotNet
{
	/// <summary>
	/// TODO: Instruction pattern tests. Test offsets.
	/// </summary>
	[TestFixture]
	public class ILReaderTest : UnitTest
	{
		[Test]
		public void BasicParseTest()
		{
			int sharedvar = 100;
			Converter<int, int> del = delegate // (int obj)
								{
									int i = 500;
									i = i + sharedvar;
									List<int> list = new List<int>(234);
									list.Add(888);
									int j = Math.Max(3, 5);

									for (int n = 0; n < j; n++)
										list.Add(n);
									return i;
								};

			ILReader r = new ILReader(del.Method);
			int icount = 0;
			while (r.Read())
			{
				if (icount > 100)
					throw new Exception("Too many instructions.");

				icount++;
			}

			if (icount < 5)
				throw new Exception("too few instructions.");
		}

		private delegate void BasicTestDelegate();

		[Test]
		public void TestLoadInt32()
		{
			BasicTestDelegate del = delegate
			                        	{
			                        		int i = 0x0a0b0c0d;
			                        		Math.Abs(i);
			                        	};
			ILReader r = new ILReader(del.Method);

			while (r.Read())
			{
				if (r.OpCode != IROpCodes.Ldc_I4)
					continue;
				int val = (int) r.Operand;
				AreEqual(0x0a0b0c0d, val);
				return;
			}

			Fail();
		}

		[Test]
		public void TestLoadInt64()
		{
			BasicTestDelegate del = delegate
										{
											long i = 0x0102030405060708L;
											Math.Abs(i);
										};
			ILReader r = new ILReader(del.Method);

			while (r.Read())
			{
				if (r.OpCode != IROpCodes.Ldc_I8)
					continue;
				long val = (long)r.Operand;
				AreEqual(0x0102030405060708L, val);
				return;
			}

			Fail();
		}

		[Test]
		public void TestLoadString()
		{
			BasicTestDelegate del = delegate
										{
											string s = "hey";
											s.ToString();
										};
			ILReader r = new ILReader(del.Method);

			while (r.Read())
			{
				if (r.OpCode != IROpCodes.Ldstr)
					continue;
				string s = (string) r.Operand;
				AreEqual("hey", s);
				return;
			}

			Fail();
		}

		[Test]
		public void TestLoadFloat()
		{
			BasicTestDelegate del = delegate
										{
											float s = 4.5f;
											s.ToString();
										};
			ILReader r = new ILReader(del.Method);

			while (r.Read())
			{
				if (r.OpCode != IROpCodes.Ldc_R4)
					continue;
				float f = (float) r.Operand;
				AreEqual(4.5f, f);
				return;
			}

			Fail();
		}

		[Test]
		public void TestLoadDouble()
		{
			BasicTestDelegate del = delegate
										{
											double s = 4.5d;
											s.ToString();
										};
			ILReader r = new ILReader(del.Method);

			while (r.Read())
			{
				if (r.OpCode != IROpCodes.Ldc_R8)
					continue;
				double d = (double)r.Operand;
				AreEqual(4.5d, d);
				return;
			}

			Fail();
		}

		[Test]
		public void TestVariableStack()
		{
			MemoryStream il = new MemoryStream();
			BinaryWriter w = new BinaryWriter(il);

			// A hard-coded version of Math.Max.

			// Load "arguments".
			w.Write((byte)OpCodes.Ldc_I4_1.Value);
			w.Write((byte)OpCodes.Ldc_I4_5.Value);

			// offset 2.
			w.Write((byte)OpCodes.Ble_S.Value);
			w.Write((byte) 4);

			// offset 4.
			w.Write((byte)OpCodes.Ldc_I4_1.Value);
			w.Write((byte)OpCodes.Br_S.Value);
			w.Write((byte) 2);

			// offset 7.
			w.Write((byte)OpCodes.Ldc_I4_5.Value);

			// offset 8.
			w.Write((byte)OpCodes.Ret.Value);

			ILReader reader = new ILReader(il.ToArray());
			while (reader.Read())
				; // nothing.

			AreEqual(7, reader.InstructionsRead);
		}
	}
}

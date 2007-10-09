namespace CellDotNet
{
	public static class SpuMath
	{
		public static int Abs(int value)
		{
			if (value > 0)
				return value;
			else
				return -value;
		}

		public static int Min(int value1, int value2)
		{
			if (value1 < value2)
				return value1;
			else
				return value2;
		}

		public static int Max(int value1, int value2)
		{
			if (value1 < value2)
				return value2;
			else
				return value1;
		}

		public static float Abs(float value)
		{
			if (value > 0)
				return value;
			else
				return -value;
		}

		public static float Min(float value1, float value2)
		{
			if (value1 < value2)
				return value1;
			else
				return value2;
		}

		public static float Max(float value1, float value2)
		{
			if (value1 < value2)
				return value2;
			else
				return value1;
		}

		public static Int32Vector Abs(Int32Vector value)
		{
			return SpuMath.CompareGreaterThanAndSelect(value, Int32Vector.Splat(0), value, Int32Vector.Splat(0) - value);
		}

		public static Int32Vector Min(Int32Vector value1, Int32Vector value2)
		{
			return SpuMath.CompareGreaterThanAndSelect(value1, value2, value2, value1);
		}

		public static Int32Vector Max(Int32Vector value1, Int32Vector value2)
		{
			return SpuMath.CompareGreaterThanAndSelect(value1, value2, value1, value2);
		}

		public static Float32Vector Abs(Float32Vector value)
		{
			return SpuMath.CompareGreaterThanAndSelect(value, Float32Vector.Splat(0), value, Float32Vector.Splat(0) - value);
		}

		public static Float32Vector Min(Float32Vector value1, Float32Vector value2)
		{
			return SpuMath.CompareGreaterThanAndSelect(value1, value2, value2, value1);
		}

		public static Float32Vector Max(Float32Vector value1, Float32Vector value2)
		{
			return SpuMath.CompareGreaterThanAndSelect(value1, value2, value1, value2);
		}

		internal static int Div(int dividend, int divisor)
		{
			int quotient=0, remainder=0;
			signed_divide(dividend, divisor, ref quotient, ref remainder);
			return quotient;
		}

		internal static uint Div_Un(uint dividend, uint divisor)
		{
			uint quotient = 0, remainder = 0;
			unsigned_divide(dividend, divisor, ref quotient, ref remainder);
			return quotient;
		}

		internal static int Rem(int dividend, int divisor)
		{
			int quotient = 0, remainder = 0;
			signed_divide(dividend, divisor, ref quotient, ref remainder);
			return remainder;
		}

		internal static uint Rem_Un(uint dividend, uint divisor)
		{
			uint quotient = 0, remainder = 0;
			unsigned_divide(dividend, divisor, ref quotient, ref remainder);
			return remainder;
		}

		internal static void unsigned_divide(uint dividend, uint divisor, ref uint quotient, ref uint remainder)
		{
			uint num_bits;
			uint bit, d=0;
			uint i;

			remainder = 0;
			quotient = 0;

			if (divisor == 0)
				return;

			if (divisor > dividend)
			{
				remainder = dividend;
				return;
			}

			if (divisor == dividend)
			{
				quotient = 1;
				return;
			}

			num_bits = 32;

			while (remainder < divisor)
			{
				bit = (dividend & 0x80000000) >> 31;
				remainder = (remainder << 1) | bit;
				d = dividend;
				dividend = dividend << 1;
				num_bits--;
			}

			dividend = d;
			remainder = remainder >> 1;
			num_bits++;

			for (i = 0; i < num_bits; i++)
			{
				bit = (dividend & 0x80000000) >> 31;
				remainder = (remainder << 1) | bit;
				uint t = remainder - divisor;
				bool q = ((t & 0x80000000) >> 31) == 0;
				dividend = dividend << 1;
				quotient = ((quotient << 1) | (uint)(q?1:0));
				if (q)
				{
					remainder = t;
				}
			}
		} /* unsigned_divide */

		internal static void signed_divide(int dividend, int divisor, ref int quotient, ref int remainder)
		{
			uint dend, dor;
			uint quo=0, rem=0;

			dend = (uint)(dividend < 0 ? -dividend : dividend);
			dor = (uint)(divisor < 0 ? -divisor : divisor);
//			dend = ABS(dividend);
//			dor = ABS(divisor);
			unsigned_divide(dend, dor, ref quo, ref rem);

			int q = (int) quo;

			int r = (int) rem;

			quotient = q;
			if (dividend < 0)
			{
				remainder = -r;
				if (divisor > 0)
					quotient = -q;
			}
			else
			{
				/* positive dividend */
				remainder = r;
				if (divisor < 0)
					quotient = -q;
			}
		} /* signed_divide */

		[SpuOpCode(SpuOpCodeEnum.Cflts)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Int32Vector ConvertToInteger(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v)
		{
			Int32Vector r = new Int32Vector((int)v.E1, (int)v.E2, (int)v.E3, (int)v.E4);

			return r;
		}

		[SpuOpCode(SpuOpCodeEnum.Csflt)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector ConvertToFloat(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Int32Vector v)
		{
			Float32Vector r = new Float32Vector(v.E1, v.E2, v.E3, v.E4);

			return r;
		}

		/// <summary>
		/// Compare if each word in vector <code>c1</code> is greater than the word in vector <code>c2</code>
		/// and select the corosponding word from <code>e1</code> if the comparsion is true, othervise selects
		/// the corresponding word from vector <code>e2</code>.
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="e1"></param>
		/// <param name="e2"></param>
		/// <returns></returns>
		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareAndSelectInt)]
		public static Int32Vector CompareGreaterThanAndSelect(Int32Vector c1, Int32Vector c2, Int32Vector e1, Int32Vector e2)
		{
			int r1 = c1.E1 > c2.E1 ? e1.E1 : e2.E1;
			int r2 = c1.E2 > c2.E2 ? e1.E2 : e2.E2;
			int r3 = c1.E3 > c2.E3 ? e1.E3 : e2.E3;
			int r4 = c1.E4 > c2.E4 ? e1.E4 : e2.E4;

			return new Int32Vector(r1, r2, r3, r4);
		}

		/// <summary>
		/// Compare if each word in vector <code>c1</code> is greater than the word in vector <code>c2</code>
		/// and select the corosponding word from <code>e1</code> if the comparsion is true, othervise selects
		/// the corresponding word from vector <code>e2</code>.
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="e1"></param>
		/// <param name="e2"></param>
		/// <returns></returns>
		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareAndSelectInt)]
		public static Float32Vector CompareGreaterThanAndSelect(Int32Vector c1, Int32Vector c2, Float32Vector e1, Float32Vector e2)
		{
			float r1 = c1.E1 > c2.E1 ? e1.E1 : e2.E1;
			float r2 = c1.E2 > c2.E2 ? e1.E2 : e2.E2;
			float r3 = c1.E3 > c2.E3 ? e1.E3 : e2.E3;
			float r4 = c1.E4 > c2.E4 ? e1.E4 : e2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}

		/// <summary>
		/// Compare if each word in vector <code>c1</code> is greater than the word in vector <code>c2</code>
		/// and select the corosponding word from <code>e1</code> if the comparsion is true, othervise selects
		/// the corresponding word from vector <code>e2</code>.
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="e1"></param>
		/// <param name="e2"></param>
		/// <returns></returns>
		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareAndSelectFloat)]
		public static Int32Vector CompareGreaterThanAndSelect(Float32Vector c1, Float32Vector c2, Int32Vector e1, Int32Vector e2)
		{
			int r1 = c1.E1 > c2.E1 ? e1.E1 : e2.E1;
			int r2 = c1.E2 > c2.E2 ? e1.E2 : e2.E2;
			int r3 = c1.E3 > c2.E3 ? e1.E3 : e2.E3;
			int r4 = c1.E4 > c2.E4 ? e1.E4 : e2.E4;

			return new Int32Vector(r1, r2, r3, r4);
		}

		/// <summary>
		/// Compare if each word in vector <code>c1</code> is greater than the word in vector <code>c2</code>
		/// and select the corosponding word from <code>e1</code> if the comparsion is true, othervise selects
		/// the corresponding word from vector <code>e2</code>.
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="e1"></param>
		/// <param name="e2"></param>
		/// <returns></returns>
		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareAndSelectFloat)]
		public static Float32Vector CompareGreaterThanAndSelect(Float32Vector c1, Float32Vector c2, Float32Vector e1, Float32Vector e2)
		{
			float r1 = c1.E1 > c2.E1 ? e1.E1 : e2.E1;
			float r2 = c1.E2 > c2.E2 ? e1.E2 : e2.E2;
			float r3 = c1.E3 > c2.E3 ? e1.E3 : e2.E3;
			float r4 = c1.E4 > c2.E4 ? e1.E4 : e2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareEqualsAndSelectInt)]
		public static Int32Vector CompareEqualsAndSelect(Int32Vector c1, Int32Vector c2, Int32Vector e1, Int32Vector e2)
		{
			int r1 = c1.E1 == c2.E1 ? e1.E1 : e2.E1;
			int r2 = c1.E2 == c2.E2 ? e1.E2 : e2.E2;
			int r3 = c1.E3 == c2.E3 ? e1.E3 : e2.E3;
			int r4 = c1.E4 == c2.E4 ? e1.E4 : e2.E4;

			return new Int32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Vector_CompareEqualsAndSelectInt)]
		public static Float32Vector CompareEqualsAndSelect(Int32Vector c1, Int32Vector c2, Float32Vector e1, Float32Vector e2)
		{
			float r1 = c1.E1 == c2.E1 ? e1.E1 : e2.E1;
			float r2 = c1.E2 == c2.E2 ? e1.E2 : e2.E2;
			float r3 = c1.E3 == c2.E3 ? e1.E3 : e2.E3;
			float r4 = c1.E4 == c2.E4 ? e1.E4 : e2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}
	}
}

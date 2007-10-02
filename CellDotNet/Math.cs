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

		public static int Div(int dividend, int divisor)
		{
			int quotient=0, remainder=0;
			signed_divide(dividend, divisor, ref quotient, ref remainder);
			return quotient;
		}

		public static uint Div_Un(uint dividend, uint divisor)
		{
			uint quotient = 0, remainder = 0;
			unsigned_divide(dividend, divisor, ref quotient, ref remainder);
			return quotient;
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


		internal static uint Div_Un_DEBUG(uint dividend, uint divisor)
		{
			uint quotient = 0, remainder = 0;
			unsigned_divide_DEBUG(dividend, divisor, ref quotient, ref remainder);
			return quotient;
		}

		internal static void unsigned_divide_DEBUG(uint dividend, uint divisor, ref uint quotient, ref uint remainder)
		{
			uint num_bits;
			uint bit, d = 0;
			int i;

//			remainder = 0;
//			quotient = 0;

//			if (divisor == 0)
//				return;
//
//			if (divisor > dividend)
//			{
//				remainder = dividend;
//				return;
//			}
//
//			if (divisor == dividend)
//			{
//				quotient = 1;
//				return;
//			}

			num_bits = 32;

//			while (remainder < divisor)
//			{
//				bit = (dividend & 0x80000000) >> 31;
//				remainder = (remainder << 1) | bit;
//				d = dividend;
//				dividend = dividend << 1;
//				num_bits--;
//			}

//			dividend = d;
//			remainder = remainder >> 1;
//			num_bits++;

			for (i = 0; i < num_bits; i++)
			{
//				bit = (dividend & 0x80000000) >> 31;
//				remainder = (remainder << 1) | bit;
				uint t = remainder - divisor;
				bool q = ((t & 0x80000000) >> 31) == 0;
//				dividend = dividend << 1;
				quotient = ((quotient << 1) | (uint)(q ? 1 : 0));
//				if (q)
//				{
//					remainder = t;
//				}
			}
		} /* unsigned_divide */

	}
}

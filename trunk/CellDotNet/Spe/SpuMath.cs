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

using System;

namespace CellDotNet.Spe
{
	/// <summary>
	/// Some of the functions in this class are taken from "SIMD Math Library Specification for Cell Broadband Engine Architecture" v1.1, and therefore
	/// behaves exactly as it does.
	/// </summary>
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

		[SpeResource("divd2", true)]
		public static double Div(double x, double y)
		{
			return x/y;
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

		[IntrinsicMethod(SpuIntrinsicMethod.ConvertFloatToInteger)]
		public static Int32Vector ConvertToInteger(Float32Vector v)
		{
			Int32Vector r = new Int32Vector((int)v.E1, (int)v.E2, (int)v.E3, (int)v.E4);

			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ConvertIntToFloat)]
		public static Float32Vector ConvertToFloat(Int32Vector v)
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
		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanIntAndSelect)]
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
		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanIntAndSelect)]
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
		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanFloatAndSelect)]
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
		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanFloatAndSelect)]
		public static Float32Vector CompareGreaterThanAndSelect(Float32Vector c1, Float32Vector c2, Float32Vector e1, Float32Vector e2)
		{
			float r1 = c1.E1 > c2.E1 ? e1.E1 : e2.E1;
			float r2 = c1.E2 > c2.E2 ? e1.E2 : e2.E2;
			float r3 = c1.E3 > c2.E3 ? e1.E3 : e2.E3;
			float r4 = c1.E4 > c2.E4 ? e1.E4 : e2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareEqualsIntAndSelect)]
		public static Int32Vector CompareEqualsAndSelect(Int32Vector c1, Int32Vector c2, Int32Vector e1, Int32Vector e2)
		{
			int r1 = c1.E1 == c2.E1 ? e1.E1 : e2.E1;
			int r2 = c1.E2 == c2.E2 ? e1.E2 : e2.E2;
			int r3 = c1.E3 == c2.E3 ? e1.E3 : e2.E3;
			int r4 = c1.E4 == c2.E4 ? e1.E4 : e2.E4;

			return new Int32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareEqualsIntAndSelect)]
		public static Float32Vector CompareEqualsAndSelect(Int32Vector c1, Int32Vector c2, Float32Vector e1, Float32Vector e2)
		{
			float r1 = c1.E1 == c2.E1 ? e1.E1 : e2.E1;
			float r2 = c1.E2 == c2.E2 ? e1.E2 : e2.E2;
			float r3 = c1.E3 == c2.E3 ? e1.E3 : e2.E3;
			float r4 = c1.E4 == c2.E4 ? e1.E4 : e2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareEqualsIntAndSelect)]
		public static int CompareEqualsAndSelect(int c1, int c2, int e1, int e2)
		{
			return c1 == c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareEqualsIntAndSelect)]
		public static float CompareEqualsAndSelect(int c1, int c2, float e1, float e2)
		{
			return c1 == c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanIntAndSelect)]
		public static int CompareGreaterThanAndSelect(int c1, int c2, int e1, int e2)
		{
			return c1 > c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanIntAndSelect)]
		public static float CompareGreaterThanAndSelect(int c1, int c2, float e1, float e2)
		{
			return c1 > c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanFloatAndSelect)]
		public static int CompareGreaterThanAndSelect(float c1, float c2, int e1, int e2)
		{
			return c1 > c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.CompareGreaterThanFloatAndSelect)]
		public static float CompareGreaterThanAndSelect(float c1, float c2, float e1, float e2)
		{
			return c1 > c2 ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ConditionalSelectWord)]
		public static int ConditionalSelect(bool c, int e1, int e2)
		{
			return c ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ConditionalSelectWord)]
		public static float ConditionalSelect(bool c, float e1, float e2)
		{
			return c ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ConditionalSelectVector)]
		public static Int32Vector ConditionalSelect(bool c, Int32Vector e1, Int32Vector e2)
		{
			return c ? e1 : e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ConditionalSelectVector)]
		public static Float32Vector ConditionalSelect(bool c, Float32Vector e1, Float32Vector e2)
		{
			return c ? e1 : e2;
		}

		[SpuOpCode(SpuOpCodeEnum.Fma)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector MultiplyAdd(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2,
			[SpuInstructionPart(SpuInstructionPart.Rc)]Float32Vector v3)
		{
			return (v1 * v2) + v3;
		}

		[SpeResource("sinf4", false)]
		public static Float32Vector Sin(Float32Vector v)
		{
			return new Float32Vector((float)Math.Sin(v.E1), (float)Math.Sin(v.E2), (float)Math.Sin(v.E3), (float)Math.Sin(v.E4));
		}

		[SpeResource("cosf4", false)]
		public static Float32Vector Cos(Float32Vector v)
		{
			return new Float32Vector((float)Math.Cos(v.E1), (float)Math.Cos(v.E2), (float)Math.Cos(v.E3), (float)Math.Cos(v.E4));
		}

		[SpeResource("tanf4", false)]
		public static Float32Vector Tan(Float32Vector v)
		{
			return new Float32Vector((float)Math.Tan(v.E1), (float)Math.Tan(v.E2), (float)Math.Tan(v.E3), (float)Math.Tan(v.E4));
		}

		/// <summary>
		/// Seems like asinf4 doesn't return NaN for elements which are out of range (-1 .. 1).
		/// </summary>
		[SpeResource("asinf4", false)]
		public static Float32Vector Asin(Float32Vector v)
		{
			return new Float32Vector((float)Math.Asin(v.E1), (float)Math.Asin(v.E2)	, (float)Math.Asin(v.E3), (float)Math.Asin(v.E4));
		}

		[SpeResource("acosf4", false)]
		public static Float32Vector Acos(Float32Vector v)
		{
			return new Float32Vector((float)Math.Acos(v.E1), (float)Math.Acos(v.E2), (float)Math.Acos(v.E3), (float)Math.Acos(v.E4));
		}

		[SpeResource("atanf4", false)]
		public static Float32Vector Atan(Float32Vector v)
		{
			return new Float32Vector((float)Math.Atan(v.E1), (float)Math.Atan(v.E2), (float)Math.Atan(v.E3), (float)Math.Atan(v.E4));
		}

		[SpeResource("atan2f4", false)]
		public static Float32Vector Atan2(Float32Vector x, Float32Vector y)
		{
			return new Float32Vector((float)Math.Atan2(x.E1, y.E1), (float)Math.Atan2(x.E2, y.E2), (float)Math.Atan2(x.E3, y.E3), (float)Math.Atan2(x.E4, y.E4));
		}

		[SpeResource("remainderf4", false)]
		[Obsolete("remainderf4 doesn't what the rem opcode does - it doesn't return negative numbers. Need another implementation.")]
		public static Float32Vector Rem(Float32Vector x, Float32Vector y)
		{
			return new Float32Vector(x.E1 % y.E1, x.E2 % y.E2, x.E3 % y.E3, x.E4 % y.E4);
		}

		[SpeResource("sind2", true)]
		public static Float64Vector Sin(Float64Vector v)
		{
			return new Float64Vector(Math.Sin(v.E1), Math.Sin(v.E2));
		}

		[SpeResource("cosd2", true)]
		public static Float64Vector Cos(Float64Vector v)
		{
			return new Float64Vector(Math.Cos(v.E1), Math.Cos(v.E2));
		}

		[SpeResource("tand2", true)]
		public static Float64Vector Tan(Float64Vector v)
		{
			return new Float64Vector(Math.Tan(v.E1), Math.Tan(v.E2));
		}

		[SpeResource("asind2", true)]
		public static Float64Vector Asin(Float64Vector v)
		{
			return new Float64Vector(Math.Asin(v.E1), Math.Asin(v.E2));
		}

		[SpeResource("acosd2", true)]
		public static Float64Vector Acos(Float64Vector v)
		{
			return new Float64Vector(Math.Acos(v.E1), Math.Acos(v.E2));
		}

		[SpeResource("atand2", true)]
		public static Float64Vector Atan(Float64Vector v)
		{
			return new Float64Vector(Math.Atan(v.E1), Math.Atan(v.E2));
		}

		[SpeResource("atan2d2", true)]
		public static Float64Vector Atan2(Float64Vector x, Float64Vector y)
		{
			return new Float64Vector(Math.Atan2(x.E1, y.E1), Math.Atan2(x.E2, y.E2));
		}
	}
}

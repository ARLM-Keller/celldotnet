using System;

namespace CellDotNet
{
	public struct Float32Vector
	{
		private float e1, e2, e3, e4;

		public Float32Vector(float e1, float e2, float e3, float e4)
		{
			this.e1 = e1;
			this.e2 = e2;
			this.e3 = e3;
			this.e4 = e4;
		}

		[SpuOpCode(SpuOpCodeEnum.Fa)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			Float32Vector r = new Float32Vector();

			r.e1 = v1.e1 + v2.e1;
			r.e2 = v1.e2 + v2.e2;
			r.e3 = v1.e3 + v2.e3;
			r.e4 = v1.e4 + v2.e4;

			return r;
		}

		[SpuOpCode(SpuOpCodeEnum.Fs)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator -(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			Float32Vector r = new Float32Vector();

			r.e1 = v1.e1 - v2.e1;
			r.e2 = v1.e2 - v2.e2;
			r.e3 = v1.e3 - v2.e3;
			r.e4 = v1.e4 - v2.e4;

			return r;
		}

		[SpuOpCode(SpuOpCodeEnum.Fm)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator *(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			Float32Vector r = new Float32Vector();

			r.e1 = v1.e1 * v2.e1;
			r.e2 = v1.e2 * v2.e2;
			r.e3 = v1.e3 * v2.e3;
			r.e4 = v1.e4 * v2.e4;

			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.FloatVectorType_Equals)]
		public static bool operator ==(Float32Vector v1, Float32Vector v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2 && v1.e3 == v2.e3 && v1.e4 == v2.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.FloatVectorType_NotEquals)]
		public static bool operator !=(Float32Vector v1, Float32Vector v2)
		{
			return !(v1 == v2);
		}

		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + ", " + e3 + ", " + e4 + "}";
		}
	}

	public struct Int32Vector
	{
		public Int32 e1, e2, e3, e4;

		public Int32Vector(int e1, int e2, int e3, int e4)
		{
			this.e1 = e1;
			this.e2 = e2;
			this.e3 = e3;
			this.e4 = e4;
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Int32Vector operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Int32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Int32Vector v2)
		{
			Int32Vector r;

			r.e1 = v1.e1 + v2.e1;
			r.e2 = v1.e2 + v2.e2;
			r.e3 = v1.e3 + v2.e3;
			r.e4 = v1.e4 + v2.e4;

			return r;
		}

		[SpuOpCode(SpuOpCodeEnum.Sf)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Int32Vector operator -(
			[SpuInstructionPart(SpuInstructionPart.Rb)]Int32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Ra)]Int32Vector v2)
		{
			Int32Vector r;

			r.e1 = v1.e1 - v2.e1;
			r.e2 = v1.e2 - v2.e2;
			r.e3 = v1.e3 - v2.e3;
			r.e4 = v1.e4 - v2.e4;

			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.IntVectorType_Equals)]
		public static bool operator ==(Int32Vector v1, Int32Vector v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2 && v1.e3 == v2.e3 && v1.e4 == v2.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.IntVectorType_NotEquals)]
		public static bool operator !=(Int32Vector v1, Int32Vector v2)
		{
			return !(v1 == v2);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE1)]
		public static int getE1(Int32Vector v)
		{
			return v.e1;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE2)]
		public static int getE2(Int32Vector v)
		{
			return v.e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE3)]
		public static int getE3(Int32Vector v)
		{
			return v.e3;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE4)]
		public static int getE4(Int32Vector v)
		{
			return v.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE1)]
		public static Int32Vector putE1(Int32Vector v, Int32 i)
		{
			Int32Vector r = new Int32Vector(i, v.e2, v.e3, v.e4);
			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE2)]
		public static Int32Vector putE2(Int32Vector v, Int32 i)
		{
			Int32Vector r = new Int32Vector(v.e1, i, v.e3, v.e4);
			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE3)]
		public static Int32Vector putE3(Int32Vector v, Int32 i)
		{
			Int32Vector r = new Int32Vector(v.e1, v.e2, i, v.e4);
			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE4)]
		public static Int32Vector putE4(Int32Vector v, Int32 i)
		{
			Int32Vector r = new Int32Vector(v.e1, v.e2, v.e3, i);
			return r;
		}

		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + ", " + e3 + ", " + e4 +  "}";
		}
	}
}

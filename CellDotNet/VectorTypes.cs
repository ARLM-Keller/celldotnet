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

		public float E1
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE1)]
			get { return e1; }
		}

		public float E2
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE2)]
			get { return e2; }
		}

		public float E3
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE3)]
			get { return e3; }
		}

		public float E4
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE4)]
			get { return e4; }
		}


		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + ", " + e3 + ", " + e4 + "}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Float32Vector)) return false;
			Float32Vector float32Vector = (Float32Vector) obj;
			if (e1 != float32Vector.e1) return false;
			if (e2 != float32Vector.e2) return false;
			if (e3 != float32Vector.e3) return false;
			if (e4 != float32Vector.e4) return false;
			return true;
		}

		public override int GetHashCode()
		{
			int result = e1.GetHashCode();
			result = 29*result + e2.GetHashCode();
			result = 29*result + e3.GetHashCode();
			result = 29*result + e4.GetHashCode();
			return result;
		}
	}

	public struct Int32Vector
	{
		private int e1, e2, e3, e4;

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

		public int E1
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE1)]
			get { return e1; }
		}

		public int E2
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE2)]
			get { return e2; }
		}

		public int E3
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE3)]
			get { return e3; }
		}

		public int E4
		{
			[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE4)]
			get { return e4; }
		}

//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE3)]
//		public static int GetE3(Int32Vector v)
//		{
//			return v.e3;
//		}
//
//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_getE4)]
//		public static int GetE4(Int32Vector v)
//		{
//			return v.e4;
//		}
//
//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE1)]
//		public static Int32Vector PutE1(Int32Vector v, Int32 i)
//		{
//			Int32Vector r = new Int32Vector(i, v.e2, v.e3, v.e4);
//			return r;
//		}
//
//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE2)]
//		public static Int32Vector PutE2(Int32Vector v, Int32 i)
//		{
//			Int32Vector r = new Int32Vector(v.e1, i, v.e3, v.e4);
//			return r;
//		}
//
//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE3)]
//		public static Int32Vector PutE3(Int32Vector v, Int32 i)
//		{
//			Int32Vector r = new Int32Vector(v.e1, v.e2, i, v.e4);
//			return r;
//		}
//
//		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_putE4)]
//		public static Int32Vector PutE4(Int32Vector v, Int32 i)
//		{
//			Int32Vector r = new Int32Vector(v.e1, v.e2, v.e3, i);
//			return r;
//		}

		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + ", " + e3 + ", " + e4 +  "}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Int32Vector)) return false;
			Int32Vector int32Vector = (Int32Vector) obj;
			if (e1 != int32Vector.e1) return false;
			if (e2 != int32Vector.e2) return false;
			if (e3 != int32Vector.e3) return false;
			if (e4 != int32Vector.e4) return false;
			return true;
		}

		public override int GetHashCode()
		{
			int result = e1;
			result = 29*result + e2;
			result = 29*result + e3;
			result = 29*result + e4;
			return result;
		}
	}
}

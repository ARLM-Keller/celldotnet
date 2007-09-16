using System;

namespace CellDotNet
{
	internal struct Float32Vector
	{
		[SpuOpCode(SpuOpCodeEnum.Fa)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector x,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector y)
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		[SpuOpCode(SpuOpCodeEnum.Fs)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator -(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector x,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector y)
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
		}

		[SpuOpCode(SpuOpCodeEnum.Fm)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator *(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector x,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector y)
		{
			Utilities.PretendVariableIsUsed(x);
			Utilities.PretendVariableIsUsed(y);
			throw new InvalidOperationException();
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
			[SpuInstructionPart(SpuInstructionPart.Ra)]Int32Vector x,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Int32Vector y)
		{
			Int32Vector r;

			r.e1 = x.e1 - x.e1;
			r.e2 = x.e2 - x.e2;
			r.e3 = x.e3 - x.e3;
			r.e4 = x.e4 - x.e4;

			return r;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_Equals)]
		public static bool operator ==(Int32Vector v1, Int32Vector v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2 && v1.e3 == v2.e3 && v1.e4 == v2.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.VectorType_NotEquals)]
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

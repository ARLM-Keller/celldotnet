using System;
using CellDotNet.Spe;

namespace CellDotNet
{
	public struct VectorI4
	{
		private readonly int e1, e2, e3, e4;

		[IntrinsicMethod(SpuIntrinsicMethod.CombineFourWords)]
		public VectorI4(int e1, int e2, int e3, int e4)
		{
			this.e1 = e1;
			this.e2 = e2;
			this.e3 = e3;
			this.e4 = e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.SplatWord)]
		public static VectorI4 Splat(int e)
		{
			return new VectorI4(e, e, e, e);
		}

		[SpuOpCode(SpuOpCodeEnum.A)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static VectorI4 operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]VectorI4 v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]VectorI4 v2)
		{
			return new VectorI4(v1.e1 + v2.e1, v1.e2 + v2.e2, v1.e3 + v2.e3, v1.e4 + v2.e4);
		}

		[SpuOpCode(SpuOpCodeEnum.Sf)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static VectorI4 operator -(
			[SpuInstructionPart(SpuInstructionPart.Rb)]VectorI4 v1,
			[SpuInstructionPart(SpuInstructionPart.Ra)]VectorI4 v2)
		{
			return new VectorI4(v1.e1 - v2.e1, v1.e2 - v2.e2, v1.e3 - v2.e3, v1.e4 - v2.e4);
		}

		public static VectorI4 operator *(VectorI4 v1, VectorI4 v2)
		{
			return new VectorI4(v1.E1 * v2.E1, v1.E2 * v2.E2, v1.E3 * v2.E3, v1.E4 * v2.E4);
		}

		public static VectorI4 operator /(VectorI4 v1, VectorI4 v2)
		{
			return new VectorI4(v1.E1 / v2.E1, v1.E2 / v2.E2, v1.E3 / v2.E3, v1.E4 / v2.E4);
		}

		public static VectorI4 operator %(VectorI4 v1, VectorI4 v2)
		{
			return new VectorI4(v1.E1 % v2.E1, v1.E2 % v2.E2, v1.E3 % v2.E3, v1.E4 % v2.E4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Int_Equals)]
		public static bool operator ==(VectorI4 v1, VectorI4 v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2 && v1.e3 == v2.e3 && v1.e4 == v2.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Int_NotEquals)]
		public static bool operator !=(VectorI4 v1, VectorI4 v2)
		{
			return !(v1 == v2);
		}

		public int E1
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord0)]
			get { return e1; }
		}

		public int E2
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord1)]
			get { return e2; }
		}

		public int E3
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord2)]
			get { return e3; }
		}

		public int E4
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord3)]
			get { return e4; }
		}

		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + ", " + e3 + ", " + e4 + "}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is VectorI4)) return false;
			VectorI4 other = (VectorI4)obj;
			return this == other;
		}

		public override int GetHashCode()
		{
			int result = e1;
			result = 29 * result + e2;
			result = 29 * result + e3;
			result = 29 * result + e4;
			return result;
		}
	}
}

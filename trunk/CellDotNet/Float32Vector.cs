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

using CellDotNet.Spe;

namespace CellDotNet
{
	public struct Float32Vector
	{
		private readonly float e1, e2, e3, e4;

		[IntrinsicMethod(SpuIntrinsicMethod.CombineFourWords)]
		public Float32Vector(float e1, float e2, float e3, float e4)
		{
			this.e1 = e1;
			this.e2 = e2;
			this.e3 = e3;
			this.e4 = e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.SplatWord)]
		public static Float32Vector Splat(float e)
		{
			return new Float32Vector(e, e, e, e);
		}

		[SpuOpCode(SpuOpCodeEnum.Fa)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			return new Float32Vector(v1.e1 + v2.e1, v1.e2 + v2.e2, v1.e3 + v2.e3, v1.e4 + v2.e4);
		}

		[SpuOpCode(SpuOpCodeEnum.Fs)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator -(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			return new Float32Vector(v1.e1 - v2.e1, v1.e2 - v2.e2, v1.e3 - v2.e3, v1.e4 - v2.e4);
		}

		[SpuOpCode(SpuOpCodeEnum.Fm)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static Float32Vector operator *(
			[SpuInstructionPart(SpuInstructionPart.Ra)]Float32Vector v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]Float32Vector v2)
		{
			return new Float32Vector(v1.e1 * v2.e1, v1.e2 * v2.e2, v1.e3 * v2.e3, v1.e4 * v2.e4);
		}

		// TODO can probably be implemented much more faster
		public static Float32Vector operator /(Float32Vector v1, Float32Vector v2)
		{
			float r1 = v1.E1 / v2.E1;
			float r2 = v1.E2 / v2.E2;
			float r3 = v1.E3 / v2.E3;
			float r4 = v1.E4 / v2.E4;

			return new Float32Vector(r1, r2, r3, r4);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Float_Equals)]
		public static bool operator ==(Float32Vector v1, Float32Vector v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2 && v1.e3 == v2.e3 && v1.e4 == v2.e4;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Float_NotEquals)]
		public static bool operator !=(Float32Vector v1, Float32Vector v2)
		{
			return !(v1 == v2);
		}

		public float E1
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord0)]
			get { return e1; }
		}

		public float E2
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord1)]
			get { return e2; }
		}

		public float E3
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetWord2)]
			get { return e3; }
		}

		public float E4
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
}

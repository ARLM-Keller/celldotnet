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
using System.Linq;
using CellDotNet.Spe;

namespace CellDotNet
{
	public struct VectorD2
	{
		private readonly double e1, e2;

		[IntrinsicMethod(SpuIntrinsicMethod.CombineTwoDWords)]
		public VectorD2(double e1, double e2)
		{
			this.e1 = e1;
			this.e2 = e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.SplatDWord)]
		public static VectorD2 Splat(double e)
		{
			return new VectorD2(e, e);
		}

		[SpuOpCode(SpuOpCodeEnum.Dfa)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static VectorD2 operator +(
			[SpuInstructionPart(SpuInstructionPart.Ra)]VectorD2 v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]VectorD2 v2)
		{
			return new VectorD2(v1.e1 + v2.e1, v1.e2 + v2.e2);
		}

		[SpuOpCode(SpuOpCodeEnum.Dfs)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static VectorD2 operator -(
			[SpuInstructionPart(SpuInstructionPart.Ra)]VectorD2 v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]VectorD2 v2)
		{
			return new VectorD2(v1.e1 - v2.e1, v1.e2 - v2.e2);
		}

		[SpuOpCode(SpuOpCodeEnum.Dfm)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		public static VectorD2 operator *(
			[SpuInstructionPart(SpuInstructionPart.Ra)]VectorD2 v1,
			[SpuInstructionPart(SpuInstructionPart.Rb)]VectorD2 v2)
		{
			return new VectorD2(v1.e1 * v2.e1, v1.e2 * v2.e2);
		}

		[SpeResource("divd2", true)]
		public static VectorD2 operator /(VectorD2 v1, VectorD2 v2)
		{
			return new VectorD2(v1.E1 / v2.E1, v1.E2 / v2.E2);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Double_Equals)]
		public static bool operator ==(VectorD2 v1, VectorD2 v2)
		{
			return v1.e1 == v2.e1 && v1.e2 == v2.e2;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Double_NotEquals)]
		public static bool operator !=(VectorD2 v1, VectorD2 v2)
		{
			return !(v1 == v2);
		}

		public double E1
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetDWord0)]
			get { return e1; }
		}

		public double E2
		{
			[IntrinsicMethod(SpuIntrinsicMethod.Vector_GetDWord1)]
			get { return e2; }
		}

		public override string ToString()
		{
			return "{" + e1 + ", " + e2 + "}";
		}

		public override bool Equals(object obj)
		{
			if (!(obj is VectorD2)) return false;
			VectorD2 other = (VectorD2)obj;
			return other == this;
		}

		public override int GetHashCode()
		{
			return 29 * e1.GetHashCode() + e2.GetHashCode();
		}
	}
}

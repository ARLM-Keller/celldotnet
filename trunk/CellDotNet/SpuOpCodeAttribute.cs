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
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// This attribute indicates that the method to which it is applied should be translated directly into the
	/// specified SPU opcode.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class SpuOpCodeAttribute : Attribute
	{
		private SpuOpCodeEnum _spuOpCode;
		public SpuOpCodeEnum SpuOpCode
		{
			get { return _spuOpCode; }
		}

		public SpuOpCodeAttribute(SpuOpCodeEnum opcode)
		{
			_spuOpCode = opcode;
		}
	}

	/// <summary>
	/// Indicates which SPU instruction part that a parameter or return value should go into.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	sealed class SpuInstructionPartAttribute : Attribute
	{
		private SpuInstructionPart _part;
		public SpuInstructionPart Part
		{
			get { return _part; }
		}

		public SpuInstructionPartAttribute(SpuInstructionPart part)
		{
			_part = part;
		}
	}
}

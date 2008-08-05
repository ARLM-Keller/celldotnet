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
using System.Text;

namespace CellDotNet.Intermediate
{
	/// <summary>
	/// Groups of instructions; derived from CLI partition III; 
	/// thought to be useful for deriving types, but it seems it's not?
	/// Note that it currently doesn't contain all instructions; might be a problem ;).
	/// </summary>
	[Obsolete("Do we need this?")]
	enum OpCodeGroup
	{
		None,
		/// <summary>
		/// add, div, mul, rem, and sub
		/// 
		/// and
		/// 
		/// add.ovf, add.ovf.un, mul.ovf, mul.ovf.un, sub.ovf, sub.ovf.un
		/// </summary>
		BinaryNumeric,
		/// <summary>
		/// neg
		/// </summary>
		UnaryNumeric,
		/// <summary>
		/// ceq, cgt, cgt.un, clt, clt.un
		/// </summary>
		BinaryComparison,
		/// <summary>
		/// and, div.un, not, or, rem.un, xor
		/// </summary>
		Integer,
		/// <summary>
		/// shl, shr, shr_un
		/// </summary>
		Shift,

		/// beq, beq.s, bge, bge.s, bge.un, bge.un.s, bgt, bgt.s, bgt.un, 
		/// bgt.un.s, ble, ble.s, ble.un, ble.un.s, blt, blt.s, blt.un, blt.un.s, bne.un, bne.un.s
		BinaryBranch
	}
}

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

namespace CellDotNet.Spe
{
	/// <summary>
	/// Subclasses of this class represents the "things" that have an address, 
	/// such as a spu basic block, a funktion, a static field etc.
	/// <para>
	/// Any object must currently be 16-bytes aligned to avoid alignment issues.
	/// </para>
	/// </summary>
	abstract class ObjectWithAddress
	{
		private int _offset = -1;
		private readonly string _name;


		public virtual string Name
		{
			get { return _name ?? ""; }
		}


		protected ObjectWithAddress()
		{
		}

		protected ObjectWithAddress(string name)
		{
			_name = name;
		}

		/// <summary>
		/// Byte offset of the basic block relative to the start of the compilation set.
		/// <para>
		/// Currently all objects must be 16-bytes aligned.
		/// </para>
		/// </summary>
		public virtual int Offset
		{
			get { return _offset; }
			set
			{
				if ((value & 0xf) != 0)
					throw new ArgumentOutOfRangeException("value", "Attempt to set non-16-bytes aligned offset. Alignment: " + (value & 0xf));
				_offset = value;
			}
		}

		/// <summary>
		/// The byte size of the object.
		/// </summary>
		public abstract int Size { get; }

//		private int _alignment;
//		/// <summary>
//		/// The alignment required for this object. Must be one of 0, 4, 8 or 16. 
//		/// A value of zero means that there is no alignment requirement.
//		/// </summary>
//		public int Alignment
//		{
//			get { return _alignment; }
//			set { _alignment = value; }
//		}
	}
}

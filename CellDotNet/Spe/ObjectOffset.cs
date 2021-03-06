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
	sealed class ObjectOffset : ObjectWithAddress
	{
		private readonly ObjectWithAddress _parent;
		private readonly int _offsetFromParent;

		public ObjectOffset(ObjectWithAddress parent, int offsetFromParent)
		{
			_parent = parent;
			_offsetFromParent = offsetFromParent;
		}

		public override int Offset
		{
			get
			{
				return _parent.Offset + OffsetFromParent;
			}
			set { throw new InvalidOperationException("This is not an independant object."); }
		}

		public override int Size
		{
			get { throw new InvalidOperationException(); }
		}

		public int OffsetFromParent
		{
			get { return _offsetFromParent; }
		}
	}
}

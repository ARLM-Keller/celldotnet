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
	struct LocalStorageAddress : IFormattable
	{
		public LocalStorageAddress(int value)
		{
			_value = value;
		}

		private int _value;
		public int Value
		{
			get { return _value; }
		}

		public static explicit operator LocalStorageAddress(int addr)
		{
			return new LocalStorageAddress(addr);
		}

		public static explicit operator LocalStorageAddress(uint addr)
		{
			return new LocalStorageAddress((int) addr);
		}

		public static LocalStorageAddress operator+(LocalStorageAddress baseAddr, int bytes)
		{
			return new LocalStorageAddress(baseAddr._value + bytes);
		}

		public static int operator%(LocalStorageAddress baseAddr, int divisor)
		{
			return baseAddr._value % divisor;
		}

		#region IFormattable Members

		public string ToString(string format, IFormatProvider formatProvider)
		{
			return Value.ToString(format, formatProvider);
		}

		#endregion
	}
}

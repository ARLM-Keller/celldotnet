using System;
using System.Collections.Generic;

namespace CellDotNet
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

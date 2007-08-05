using System;
using System.Collections.Generic;

namespace CellDotNet
{
	struct LocalStorageAddress
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

		public static LocalStorageAddress operator+(LocalStorageAddress baseAddr, int bytes)
		{
			return new LocalStorageAddress(baseAddr._value + bytes);
		}

		public static LocalStorageAddress operator%(LocalStorageAddress baseAddr, int divisor)
		{
			return new LocalStorageAddress(baseAddr._value % divisor);
		}
	}
}

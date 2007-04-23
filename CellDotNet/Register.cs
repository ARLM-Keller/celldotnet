using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents a virtual register.
	/// </summary>
	struct Register
	{
		public Register(int _number)
		{
			this._number = _number;
		}

		private int _number;
		public int Number
		{
			get { return _number; }
		}
	}
}

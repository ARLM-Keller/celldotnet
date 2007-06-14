using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    /// <summary>
    /// Represents a virtual register.
    /// </summary>
    public class VirtualRegister
    {
        public VirtualRegister(int _number)
        {
            this._number = _number;
            this._location = null;
        }

        private int _number;
        public int Number
        {
            get { return _number; }
        }

        private StorLocation _location;
        public StorLocation Location
        {
            get { return _location; }
            set { _location = value; }
        }

		/// <summary>
		/// Used for disassembly.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "$" + Number;
		}

    }
}
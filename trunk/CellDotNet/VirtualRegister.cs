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

namespace CellDotNet
{
    /// <summary>
    /// Represents a virtual register.
    /// </summary>
    public class VirtualRegister
    {
		public VirtualRegister()
		{
		}

		/// <summary>
		/// Both this and the parameter-less ctor are ok - the register allocator doesn't use
		/// the number.
		/// </summary>
		/// <param name="_number"></param>
        public VirtualRegister(int _number)
        {
            this._number = _number;
        }

        private int _number;
        public int Number
        {
            get { return _number; }
        }

    	private bool _isRegisterSet;
		/// <summary>
		/// Indicates whether a physical register has been assigned to this virtual register.
		/// </summary>
    	public bool IsRegisterSet
    	{
			get { return _isRegisterSet; }
    	}


    	private CellRegister _register;
    	public CellRegister Register
    	{
    		get
    		{
				Utilities.AssertOperation(_isRegisterSet, "_isRegisterSet: A hardware register has not been assigned.");
    			return _register;
    		}
			set
			{
				Utilities.AssertOperation(!_isRegisterSet, "!_isRegisterSet: This register already has an assigned hardware register.");
				_isRegisterSet = true;
				_register = value;
			}
    	}

		/// <summary>
		/// Used for disassembly.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (_isRegisterSet)
			{
				if (Register == CellRegister.REG_1)
					return "$SP";
				else if (Register == CellRegister.REG_0)
					return "$LR";
				else
					return "$" + Register;
			}
			else if (Number != 0)
				return "$$" + Number;
			else
				return "$$";
		}
    }
}

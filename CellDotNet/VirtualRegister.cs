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
            _location = null;
        }

        private int _number;
        public int Number
        {
            get { return _number; }
        }

        private StoreLocation _location;
        public StoreLocation Location
        {
            get { return _location; }
            set { _location = value; }
        }

		private bool _isRegisterSet = false;
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
    			return _register;
    		}
			set
			{
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
			if (Location is HardwareRegister) //TODO old
				return "$" + ((HardwareRegister)Location).Register;
			else if (_isRegisterSet)
				return "$" + Register;
			else if (Number != 0)
				return "$$" + Number;
			else
				return "$$";
		}
    }
}

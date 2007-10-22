using System;
using System.Collections.Generic;

namespace CellDotNet
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
		private string _name;


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

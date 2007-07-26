using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Subclasses of this class represents the "things" that have an address, 
	/// such as a spu basic block, a funktion, a static field etc.
	/// </summary>
	abstract class ObjectWithAddress
	{
		private int _offset = -1;

		/// <summary>
		/// Byte offset of the basic block relative to the start of the compilation set.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

		/// <summary>
		/// The byte size of the object.
		/// </summary>
		public abstract int Size { get; }
	}
}

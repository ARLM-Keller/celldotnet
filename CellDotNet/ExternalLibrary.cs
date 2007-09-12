using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Represents an external compiled library which most likely contains methods that can be called.
	/// </summary>
	class ExternalLibrary
	{
		private int _offset;
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

		public virtual ExternalMethod ResolveMethod(string name)
		{
			throw new NotImplementedException();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;

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

		public virtual ExternalMethod ResolveMethod(MethodInfo reflectionMethod)
		{
			throw new NotImplementedException();
		}
	}
}

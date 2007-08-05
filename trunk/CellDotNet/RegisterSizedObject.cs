using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// A 16-bytes <see cref="ObjectWithAddress"/> for storing a register. 
	/// Used for storing return value from entry point method.
	/// </summary>
	class RegisterSizedObject : ObjectWithAddress
	{
		public override int Size
		{
			get { return 16; }
		}
	}
}

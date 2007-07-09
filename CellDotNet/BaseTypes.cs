using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Cecil basic .NET data types.
	/// </summary>
	class BaseTypes
	{
		static private Type _int32 = typeof(int);
		static public Type Int32
		{
			get { return _int32; }
		}

		static private Type _string = typeof(string);
		static public Type String
		{
			get { return _string; }
		}

		static private Type _int64 = typeof(long);
		static public Type Int64
		{
			get { return _int64; }
		}

		static private Type _bool = typeof(bool);
		static public Type Bool
		{
			get { return _bool; }
		}

	}
}

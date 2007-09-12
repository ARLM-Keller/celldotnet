using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	/// <summary>
	/// Represents an external compiled library which most likely contains methods that can be called.
	/// </summary>
	class Library
	{
		private int _offset;
		private byte[] _contents;

		public Library(byte[] contents)
		{
			Utilities.AssertArgumentNotNull(contents, "contents");
			Utilities.AssertArgument(contents.Length > 0, "contents.Length > 0");

			_contents = contents;
		}

		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

		public int Size
		{
			get { return _contents.Length; }
		}

		public virtual LibraryMethod ResolveMethod(MethodInfo reflectionMethod)
		{
			throw new NotImplementedException();
		}

		public byte[] GetContents()
		{
			return _contents;
		}
	}
}

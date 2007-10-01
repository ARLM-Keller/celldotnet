using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// Represents a method in an external library.
	/// </summary>
	class LibraryMethod : SpuRoutine
	{
		private Library _library;
		private int _offsetInLibrary;

		public LibraryMethod(string name, Library library, int offsetInLibrary, MethodInfo signature) : base(name, signature)
		{
			Utilities.AssertArgument(!string.IsNullOrEmpty(name), "name null");
			Utilities.AssertArgumentNotNull(library, "library");
			Utilities.AssertArgumentNotNull(offsetInLibrary, "offsetInLibrary");
			Utilities.AssertArgumentNotNull(signature, "signature");

			_library = library;
			_offsetInLibrary = offsetInLibrary;
		}

		/// <summary>
		/// The library to which this method belongs.
		/// </summary>
		public Library Library
		{
			get { return _library; }
		}

		/// <summary>
		/// The offset of the method within the library.
		/// </summary>
		public int OffsetInLibrary
		{
			get { return _offsetInLibrary; }
		}

		public override int Offset
		{
			get { return Library.Offset + _offsetInLibrary; }
			set { throw new InvalidOperationException(); }
		}

		public override int Size
		{
			get { throw new InvalidOperationException(); }
		}

		public override ReadOnlyCollection<MethodParameter> Parameters
		{
			get { return _parameters; }
		}

		public override StackTypeDescription ReturnType
		{
			get { return _returnType; }
		}
	}
}

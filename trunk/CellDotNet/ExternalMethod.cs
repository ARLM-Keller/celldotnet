using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CellDotNet
{
	/// <summary>
	/// Represents a method in an external library.
	/// </summary>
	class ExternalMethod : SpuRoutine
	{
		private ExternalLibrary _library;
		private int _offsetInLibrary;

		public ExternalMethod(string name, ExternalLibrary library, int offsetInLibrary) : base(name)
		{
			Utilities.AssertArgumentNotNull(library, "library");
			Utilities.AssertArgumentNotNull(offsetInLibrary, "offsetInLibrary");

			_library = library;
			_offsetInLibrary = offsetInLibrary;
		}

		/// <summary>
		/// The library to which this method belongs.
		/// </summary>
		public ExternalLibrary Library
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
			get { throw new NotImplementedException(); }
		}

		public override StackTypeDescription ReturnType
		{
			get { throw new NotImplementedException(); }
		}
	}
}

// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet.Spe
{
	/// <summary>
	/// Represents a method in an external library.
	/// </summary>
	sealed class LibraryMethod : SpuRoutine
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

		public override int[] Emit()
		{
			throw new NotImplementedException();
		}

		public override void PerformAddressPatching()
		{
			throw new NotImplementedException();
		}
	}
}

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
		private StackTypeDescription _returnType;
		private ReadOnlyCollection<MethodParameter> _parameters;

		public LibraryMethod(string name, Library library, int offsetInLibrary, MethodInfo signature) : base(name)
		{
			Utilities.AssertArgumentNotNull(library, "library");
			Utilities.AssertArgumentNotNull(offsetInLibrary, "offsetInLibrary");
			Utilities.AssertArgumentNotNull(signature, "signature");

			TypeDeriver td = new TypeDeriver();
			_returnType = td.GetStackTypeDescription(signature.ReturnType);
			List<MethodParameter> plist = new List<MethodParameter>();
			foreach (ParameterInfo paraminfo in signature.GetParameters())
			{
				plist.Add(new MethodParameter(paraminfo, td.GetStackTypeDescription(paraminfo.ParameterType)));
			}
			_parameters = plist.AsReadOnly();

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

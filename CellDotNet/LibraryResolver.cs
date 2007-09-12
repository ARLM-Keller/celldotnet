using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// <see cref="CompileContext"/> uses this class to find external libraries.
	/// </summary>
	abstract class LibraryResolver
	{
		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="DllNotFoundException">If the library cannot be resolved.</exception>
		/// <param name="dllImportName"></param>
		/// <returns></returns>
		public abstract Library ResolveLibrary(string dllImportName);
	}

	/// <summary>
	/// A <see cref="LibraryResolver"/> that looks for file libraries.
	/// </summary>
	class StaticFileLibraryResolver : LibraryResolver
	{
		public override Library ResolveLibrary(string dllImportName)
		{
			throw new DllNotFoundException("Cannot resolve library \"" + dllImportName + "\".");
		}
	}
}

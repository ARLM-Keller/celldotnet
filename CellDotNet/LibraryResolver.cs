using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// <see cref="CompileContext"/> uses this class to find external libraries. <see cref="StaticFileLibraryResolver"/>
	/// looks for ELF libraries on the disk.
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
}

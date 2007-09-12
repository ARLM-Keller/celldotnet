using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// <see cref="CompileContext"/> uses this class to find external libraries.
	/// </summary>
	abstract class ExternalLibraryResolver
	{
		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="DllNotFoundException">If the library cannot be resolved.</exception>
		/// <param name="dllImportName"></param>
		/// <returns></returns>
		public abstract ExternalLibrary ResolveLibrary(string dllImportName);
//		{
//			throw new NotImplementedException();
//		}
	}
}

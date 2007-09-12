using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// <see cref="CompileContext"/> uses this class to find external libraries.
	/// </summary>
	class ExternalLibraryResolver
	{
		public virtual ExternalLibrary ResolveLibrary(string dllImportName)
		{
			throw new NotImplementedException();
		}
	}
}

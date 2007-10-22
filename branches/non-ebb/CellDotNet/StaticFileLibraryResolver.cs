using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	/// <summary>
	/// A <see cref="LibraryResolver"/> that looks for file libraries.
	/// </summary>
	class StaticFileLibraryResolver : LibraryResolver
	{
		public override Library ResolveLibrary(string dllImportName)
		{
			string fullpath = FindFullLibraryPath(dllImportName);
			return new ElfLibrary(fullpath);
		}

		private string FindFullLibraryPath(string dllImportName)
		{
			string filename = dllImportName + ".a";

			if (!File.Exists(filename))
				throw new DllNotFoundException("Cannot resolve library \"" + dllImportName + "\".");

			string fullPath = Path.GetFullPath(filename);
			return fullPath;
		}
	}
}

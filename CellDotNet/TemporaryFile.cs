using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	class TemporaryFile : IDisposable
	{
		public string Path { get; private set; }

		public TemporaryFile()
		{
			Path = System.IO.Path.GetTempFileName();
		}

		public void Dispose()
		{
			if (File.Exists(Path))
				File.Delete(Path);
		}
	}
}

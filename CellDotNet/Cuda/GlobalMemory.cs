using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	public class GlobalMemory<T> : IDisposable where T : struct
	{
		public GlobalMemory(int elementCount)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public T[] GetBuffer()
		{
			throw new NotImplementedException();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	public static class CudaRuntime
	{
		static public void SyncThreads()
		{
			throw new NotImplementedException();
		}

		public static void CopyDeviceToHost<T>(GlobalMemory<T> c, T[] h_C) where T : struct
		{
			throw new NotImplementedException();
		}

		public static void CopyHostToDevice<T>(T[] a, GlobalMemory<T> d_A) where T : struct
		{
			throw new NotImplementedException();
		}
	}
}

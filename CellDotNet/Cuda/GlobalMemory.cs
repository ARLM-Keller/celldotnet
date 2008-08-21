using System;
using System.Collections.Generic;
using CellDotNet.Cuda.DriverApi;

namespace CellDotNet.Cuda
{
	internal interface IGlobalMemory
	{
		int GetDeviceAddress();
	}

	public class GlobalMemory<T> : IGlobalMemory, IDisposable where T : struct
	{
		private CUdeviceptr _handle;
		private bool _hasfreed;

		internal GlobalMemory(CUdeviceptr handle)
		{
			_handle = handle;
		}

		public void Dispose()
		{
			if (_hasfreed) 
				return;
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuMemFree(_handle);
			_hasfreed = true;
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		int IGlobalMemory.GetDeviceAddress()
		{
			return _handle.Ptr;
		}
	}
}

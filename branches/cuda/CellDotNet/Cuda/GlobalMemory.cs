using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private bool _isdisposed;

		internal GlobalMemory(CUdeviceptr handle)
		{
			_handle = handle;
		}

		public void Dispose()
		{
			if (_isdisposed)
				return;
			GC.SuppressFinalize(this);
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuMemFree(_handle);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
			_isdisposed = true;
		}

		~GlobalMemory()
		{
			Debug.WriteLine("Cannot free CUDA memory from finalizer thread.");
		}

		int IGlobalMemory.GetDeviceAddress()
		{
			return _handle.Ptr;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CellDotNet.Cuda.DriverApi;

namespace CellDotNet.Cuda
{
	public class CudaContext : IDisposable
	{
		private readonly CUcontext _handle;

		public CudaContext(CudaDevice device)
		{
//			Debugger.Break();
//			Console.WriteLine("CudaContext.ctor: device");
			var rc = DriverUnsafeNativeMethods.cuCtxCreate(out _handle, 0, device.CUdevice);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			Device = device;
		}

		internal CudaContext(CUcontext handle, CudaDevice device)
		{
//			Debugger.Break();
//			Console.WriteLine("CudaContext.ctor: handle");

			_handle = handle;
			Device = device;
		}

		public static CudaContext GetCurrent()
		{
			return GetCurrentOrNew(false);
		}

		public static CudaContext GetCurrentOrNew()
		{
			return GetCurrentOrNew(true);
		}

		private static CudaContext GetCurrentOrNew(bool createIfNecessary)
		{
			CudaDevice.EnsureCudaInitialized();
//			Console.WriteLine("CudaContext: GetCurrentOrNew: " + createIfNecessary);

			CUcontextAttachedHandle handle;
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuCtxAttach(out handle, 0);
			if (rc == DriverStatusCode.CUDA_ERROR_INVALID_CONTEXT && createIfNecessary)
			{
				return new CudaContext(CudaDevice.PreferredDevice);
			}
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			CUdevice devhandle;
			rc = DriverUnsafeNativeMethods.cuCtxGetDevice(out devhandle);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			var dev = new CudaDevice(devhandle);
			var ctx = new CudaContext(handle, dev);

			return ctx;
		}

		public void Dispose()
		{
			_handle.Dispose();
		}

		public CudaDevice Device { get; private set; }

		public void Synchronize()
		{
			var rc = DriverUnsafeNativeMethods.cuCtxSynchronize();
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		public GlobalMemory<T> AllocateLinear<T>(int count) where T : struct
		{
			uint bytecount = (uint)count*(uint)Marshal.SizeOf(typeof (T));
			CUdeviceptr dptr;
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuMemAlloc(out dptr, bytecount);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			return new GlobalMemory<T>(dptr);
		}

#if UNITTEST
		/// <summary>
		/// 
		/// </summary>
		internal IntPtr CudaHandle
		{
			get { return _handle.DangerousGetHandle(); }
		}
#endif // UNITTEST

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellDotNet.Cuda.DriverApi;

namespace CellDotNet.Cuda
{
	internal class CudaModule
	{
		private readonly CUmodule _handle;

		private CudaModule(CUmodule handle)
		{
			_handle = handle;
		}

		public static CudaModule LoadData(string cubin, CudaDevice device)
		{
			CUmodule handle;
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuModuleLoadData(out handle, cubin);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			return new CudaModule(handle);
		}

		public CudaFunction GetFunction(string name)
		{
			CUfunction func;
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuModuleGetFunction(out func, _handle, name);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			return new CudaFunction(func);
		}
	}
}

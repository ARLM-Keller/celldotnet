﻿using System;
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
		private readonly int _elementSize;

		internal int ElementSize
		{
			get { return _elementSize; }
		}

		public int Length { get; private set; }

		internal GlobalMemory(CUdeviceptr _handle, int length, int elementSize)
		{
			this._handle = _handle;
			Length = length;
			_elementSize = elementSize;
		}

		public void Dispose()
		{
			if (_isdisposed)
				return;
			GC.SuppressFinalize(this);
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuMemFree(_handle);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
			_handle = default(CUdeviceptr);
			_isdisposed = true;
		}

		int IGlobalMemory.GetDeviceAddress()
		{
			return _handle.Ptr;
		}

		internal int GetDeviceAddress()
		{
			return _handle.Ptr;
		}
	}
}
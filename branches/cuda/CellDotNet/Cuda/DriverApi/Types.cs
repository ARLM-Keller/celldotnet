using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace CellDotNet.Cuda.DriverApi
{
	/// <summary>
	/// CUdevprop.
	/// </summary>
	internal struct CUdevprop
	{
		public int MaxThreadsPerBlock;
		public int MaxThreadsDimX;
		public int MaxThreadsDimY;
		public int MaxThreadsDimZ;
		public int MaxGridSizeX;
		public int MaxGridSizeY;
		public int MaxGridSizeZ;
		public int SharedMemPerBlock;
		public int TotalConstantMemory;
		public int SimdWidth;
		public int MemPitch;
		public int RegsPerBlock;
		public int ClockRate;
		public int TextureAlign;
	}

	/// <summary>
	/// A CUDA device.
	/// </summary>
	struct CUdevice
	{
		public IntPtr IntPtr;
	}

	/// <summary>
	/// For handles retrieved with <see cref="DriverUnsafeNativeMethods.cuCtxCreate"/>.
	/// </summary>
	class CUcontext : SafeHandleZeroOrMinusOneIsInvalid
	{
		public CUcontext() : base(true)
		{
		}

		public CUcontext(bool ownsHandle)
			: base(ownsHandle)
		{
		}

		protected override void Dispose(bool disposing)
		{
			// Can't clear it up from the finalizer thread anyway. 
			// Should consider making this a non-safehandle, since IDisposable is all we really have for CUDA.
			if (!disposing)
			{
				Debug.WriteLine("Cannot clean up CUDA context from finalizer thread.");
				return;
			}
			base.Dispose(disposing);
		}

		protected override bool ReleaseHandle()
		{
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuCtxDestroy(handle);
			bool success = rc == DriverStatusCode.CUDA_SUCCESS;
			if (success)
				SetHandleAsInvalid();
			return success;
		}
	}

	/// <summary>
	/// For handles retrieved with <see cref="DriverUnsafeNativeMethods.cuCtxAttach"/>.
	/// </summary>
	class CUcontextAttachedHandle : CUcontext
	{
		public CUcontextAttachedHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuCtxDetach(handle);

			bool success = rc == DriverStatusCode.CUDA_SUCCESS;
			if (success)
				SetHandleAsInvalid();
			return success;
		}
	}

	class CUmodule : SafeHandleZeroOrMinusOneIsInvalid
	{
		public CUmodule() : base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuModuleUnload(this);
			return rc == DriverStatusCode.CUDA_SUCCESS;
		}
	}

	/// <summary>
	/// A reference to a pointer within a loaded module.
	/// </summary>
	struct CUfunction
	{
		public IntPtr IntPtr;
	}

	/// <summary>
	/// A reference to a texture within a loaded module.
	/// </summary>
	struct CUtexref
	{
		public IntPtr IntPtr;
	}

	/// <summary>
	/// A pointer to linear device memory.
	/// </summary>
	struct CUdeviceptr
	{
		public int Ptr;
	}
}
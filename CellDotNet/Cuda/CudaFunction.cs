using System;
using CellDotNet.Cuda.DriverApi;

namespace CellDotNet.Cuda
{
	internal class CudaFunction
	{
		private int? _gridWidth, _gridHeight;
		private readonly CUfunction _handle;

		public CudaFunction(CUfunction pointer)
		{
			_handle = pointer;
		}

		public void SetBlockSize(int x, int y, int z)
		{
			// TODO: Validate.
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuFuncSetBlockShape(_handle, x, y, z);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		public void SetGridSize(int x, int y)
		{
			// TODO: Validate.
			_gridWidth = x;
			_gridHeight = y;
		}

		public void Launch(object[] arguments)
		{
			if (_gridWidth == null || _gridHeight == null)
				throw new InvalidOperationException("No grid size has been set.");

			int offset = 0;
			int argidx = -1;
			DriverStatusCode rc;

			foreach (object rawArgument in arguments)
			{
				argidx++;
				object arg = ConvertArgument(rawArgument);

				if (arg is int || arg is uint)
				{
					uint value = arg is int ? (uint)(int)arg : (uint)arg;

					rc = DriverUnsafeNativeMethods.cuParamSeti(_handle, offset, value);
					DriverUnsafeNativeMethods.CheckReturnCode(rc);
					offset += 4;
					continue;
				}
				if (arg is short || arg is ushort)
				{
					uint value = arg is short ? (ushort)(short)arg : (ushort)arg;

					rc = DriverUnsafeNativeMethods.cuParamSeti(_handle, offset, value);
					DriverUnsafeNativeMethods.CheckReturnCode(rc);
					offset += 4;
					continue;
				}
				if (arg is float)
				{
					rc = DriverUnsafeNativeMethods.cuParamSetf(_handle, offset, (float)arg);
					DriverUnsafeNativeMethods.CheckReturnCode(rc);
					offset += 4;
					continue;
				}

				throw new ArgumentException("Argument no. " + argidx + " is of an unsupported type: " + arg.GetType().Name);
			}
			rc = DriverUnsafeNativeMethods.cuParamSetSize(_handle, (uint)offset);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			rc = DriverUnsafeNativeMethods.cuLaunchGrid(_handle, _gridWidth.Value, _gridHeight.Value);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		private object ConvertArgument(object argument)
		{
			if (argument is IGlobalMemory)
				return ((IGlobalMemory)argument).GetDeviceAddress();
			return argument;
		}
	}
}

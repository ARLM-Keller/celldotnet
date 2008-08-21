using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C5;
using CellDotNet.Cuda.DriverApi;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class CudaKernelTest : UnitTest
	{
		public static void TestIR_MultipleMethods_CallMethod(int arg)
		{
			// nothing.
		}

		[Test]
		public void TestIR_MultipleMethods()
		{
			Action<int> del = delegate(int i) { TestIR_MultipleMethods_CallMethod(i); };
			var kernel = CudaKernel.Create<Action<int>>(del.Method);
			kernel.PerformProcessing(CudaKernelCompileState.IRConstructionDone);

			AreEqual(2, kernel.Methods.Count);
			var q = from cm in kernel.Methods
			        from block in cm.Blocks
			        from inst in block.Instructions
			        where inst.Operand is CudaMethod
			        select new {CudaMethod = cm, Instruction = inst};

			var methodAndInst = q.Single();
			CudaMethod otherMethod = kernel.Methods.Single(cm => cm != methodAndInst.CudaMethod);
			AreEqual(otherMethod, (CudaMethod) methodAndInst.Instruction.Operand);
		}

		[Test]
		public void TestApiFeel()
		{
			Action<float[], int> del = delegate(float[] arr, int i) { arr[0] = 3.4f; };
			var kernel = CudaKernel.Create<Action<GlobalMemory<float>, int>>(del.Method);

			GlobalMemory<float> mem = kernel.Context.AllocateLinear<float>(200);

			kernel.SetBlockShape(32, 8);
			kernel.SetGridSize(10, 10);
			kernel.ExecuteUntyped(mem, 1);
		}
	}

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
					uint value = arg is int ? (uint) (int) arg : (uint) arg;

					rc = DriverUnsafeNativeMethods.cuParamSeti(_handle, offset, value);
					DriverUnsafeNativeMethods.CheckReturnCode(rc);
					offset += 4;
					continue;
				}
				if (arg is float)
				{
					rc = DriverUnsafeNativeMethods.cuParamSetf(_handle, offset, (float) arg);
					DriverUnsafeNativeMethods.CheckReturnCode(rc);
					offset += 4;
					continue;
				}

				throw new ArgumentException("Argument no. " + argidx + " is of an unsupported type: " + arg.GetType().Name);
			}
			rc = DriverUnsafeNativeMethods.cuParamSetSize(_handle, (uint) offset);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

			rc = DriverUnsafeNativeMethods.cuLaunchGrid(_handle, _gridWidth.Value, _gridHeight.Value);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		private object ConvertArgument(object argument)
		{
			if (argument is IGlobalMemory)
				return ((IGlobalMemory) argument).GetDeviceAddress();
			return argument;
		}
	}
}

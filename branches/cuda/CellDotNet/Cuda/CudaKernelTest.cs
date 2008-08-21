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
			var kernel = CudaKernel.Create<Action<GlobalMemory<float>>>(del.Method);
//			CudaKernel<Action<int>> kernel = CudaKernel.Create(del);

			GlobalMemory<float> mem = kernel.Context.AllocateLinear<float>(200);

			kernel.SetBlockSize(32, 8);
			kernel.SetGridSize(10, 10);
			kernel.Execute(mem);
//			kernel.ExecuteUntyped(123);

//			kernel.PerformProcessing(CudaKernelCompileState.IRConstructionDone);

//			using (var ctx = new CudaContext())
//			{
//				Console.WriteLine("Device: " + ctx.Device.Name);
//				CudaModule module = CudaModule.LoadData(ptx);
//				CudaFunction func = module.GetFunction("mykernel");
//
//				
//			}

		}
	}

	internal class CudaModule
	{
		public static CudaModule LoadData(string cubin, CudaDevice device)
		{
			throw new NotImplementedException();
		}

		public CudaFunction GetFunction(string s)
		{
			throw new NotImplementedException();
		}
	}

	internal class CudaFunction
	{
		private int? _gridSizeX, _gridSizeY;

		public CUfunction Handle { get; private set; }
		public CudaDevice Device { get; private set; }

		public CudaFunction(CUfunction pointer)
		{
			Handle = pointer;
		}

		public void SetBlockSize(int x, int y, int z)
		{
			// TODO: Validate.
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuFuncSetBlockShape(Handle, x, y, z);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);
		}

		public void Execute(object[] arguments)
		{
			DriverStatusCode rc = DriverUnsafeNativeMethods.cuLaunchGrid(Handle, _gridSizeX.Value, _gridSizeY.Value);
			DriverUnsafeNativeMethods.CheckReturnCode(rc);

		}

		public void SetGridSize(int x, int y)
		{
			// TODO: Validate.
			_gridSizeX = x;
			_gridSizeY = y;
		}
	}
}

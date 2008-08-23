using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda.DriverApi
{
	[TestFixture]
	public class ILOpcodeExecutionTest : UnitTest
	{
		[Test]
		public void Test_Blt_F4()
		{
			Action<float[], float, float> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3.5f, 3.6f);
				VerifyExecution(kernel, 3.6f, 3.5f);
			}
		}

		[Test]
		public void Test_Blt_I4()
		{
			Action<int[], int, int> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3, 4);
				VerifyExecution(kernel, 4, 3);
				VerifyExecution(kernel, -3, 4);
				VerifyExecution(kernel, 4, -3);
			}
		}

		[Test]
		public void Test_Blt_U4()
		{
			Action<uint[], uint, uint> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3u, 4u);
				VerifyExecution(kernel, 4u, 3u);
				VerifyExecution(kernel, 3u, uint.MaxValue);
				VerifyExecution(kernel, uint.MaxValue, 3u);
			}
		}
	
		[Test]
		public void Test_Ble_F4()
		{
			Action<float[], float, float> del = (arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3.5f, 3.6f);
				VerifyExecution(kernel, 3.6f, 3.5f);
			}
		}

		[Test]
		public void Test_Ble_I4()
		{
			Action<int[], int, int> del = (arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3, 4);
				VerifyExecution(kernel, 4, 3);
				VerifyExecution(kernel, -3, 4);
				VerifyExecution(kernel, 4, -3);
			}
		}

		[Test]
		public void Test_Ble_U4()
		{
			Action<uint[], uint, uint> del = (arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3u, 4u);
				VerifyExecution(kernel, 4u, 3u);
				VerifyExecution(kernel, 3u, uint.MaxValue);
				VerifyExecution(kernel, uint.MaxValue, 3u);
			}
		}

		void VerifyExecution<T>(CudaKernel kernel, T arg1, T arg2) where T : struct
		{
			if (!CudaDevice.HasCudaDevice)
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				return;
			}

			var devmem = kernel.Context.AllocateLinear<T>(16);

			kernel.SetBlockShape(1, 1);
			kernel.SetGridSize(1, 1);
			kernel.ExecuteUntyped(devmem, arg1, arg2);

			var arr = new T[devmem.Length];
			kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

			// Invoke to got correct result.
			var refExec = new T[devmem.Length];
			kernel.KernelMethod.Invoke(null, new object[] {refExec, arg1, arg2});
			var expectedResult = refExec[0];

			if (!arr[0].Equals(expectedResult))
			{
				Console.WriteLine("Failing PTX: " + kernel.GetPtx());
			}
			AreEqual(expectedResult, arr[0]);
		}
	}
}

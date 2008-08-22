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
		public void Test_Lt_F4()
		{
			Action<float[], float, float> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3.5f, 3.6f, 3.5f);
				VerifyExecution(kernel, 3.6f, 3.5f, 3.5f);
			}
		}

		[Test]
		public void Test_Lt_I4()
		{
			Action<int[], int, int> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3, 4, 3);
				VerifyExecution(kernel, 4, 3, 3);
				VerifyExecution(kernel, -3, 4, -3);
				VerifyExecution(kernel, 4, -3, -3);
			}
		}

		[Test]
		public void Test_Lt_U4()
		{
			Action<uint[], uint, uint> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3u, 4u, 3u);
				VerifyExecution(kernel, 4u, 3u, 3u);
				VerifyExecution(kernel, 3u, uint.MaxValue, 3u);
				VerifyExecution(kernel, uint.MaxValue, 3u, 3u);
			}
		}

		[Test]
		public void Test_Lt_I2()
		{
			Action<short[], short, short> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution<short>(kernel, 3, 4, 3);
				VerifyExecution<short>(kernel, 4, 3, 3);
				VerifyExecution<short>(kernel, -3, 4, -3);
				VerifyExecution<short>(kernel, 4, -3, -3);
			}
		}

		[Test]
		public void Test_Lt_U2()
		{
			Action<ushort[], ushort, ushort> del = (arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2;

			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution<ushort>(kernel, 3, 4, 3);
				VerifyExecution<ushort>(kernel, 4, 3, 3);
				VerifyExecution<ushort>(kernel, 3, ushort.MaxValue, 3);
				VerifyExecution<ushort>(kernel, ushort.MaxValue, 3, 3);
			}
		}

		void VerifyExecution<T>(CudaKernel kernel, T arg1, T arg2, T expectedResult) where T : struct
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

			AreEqual(expectedResult, arr[0]);
		}
	}
}

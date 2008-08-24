using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class ILOpcodeExecutionTest : UnitTest
	{
		[Test]
		public void Test_Ldc_F4()
		{
			using (var kernel = CudaKernel.Create(new Action<float[]>(arr => arr[0] = 1)))
				VerifyExecution<float>(kernel);
			using (var kernel = CudaKernel.Create(new Action<float[]>(arr => arr[0] = -1)))
				VerifyExecution<float>(kernel);
		}

		[Test]
		public void Test_Ldc_F4_Unordered()
		{
			using (var kernel = CudaKernel.Create(new Action<float[]>(arr => arr[0] = float.NegativeInfinity)))
				VerifyExecution<float>(kernel);
			using (var kernel = CudaKernel.Create(new Action<float[]>(arr => arr[0] = float.PositiveInfinity)))
				VerifyExecution<float>(kernel);
			using (var kernel = CudaKernel.Create(new Action<float[]>(arr => arr[0] = float.NaN)))
				VerifyExecution<float>(kernel);
		}

		[Test]
		public void Test_Ldc_I4()
		{
			using (var kernel = CudaKernel.Create(new Action<int[]>(arr => arr[0] = 1)))
				VerifyExecution<int>(kernel);
			using (var kernel = CudaKernel.Create(new Action<int[]>(arr => arr[0] = -1)))
				VerifyExecution<int>(kernel);
			using (var kernel = CudaKernel.Create(new Action<int[]>(arr => arr[0] = int.MaxValue)))
				VerifyExecution<int>(kernel);
			using (var kernel = CudaKernel.Create(new Action<int[]>(arr => arr[0] = int.MinValue)))
				VerifyExecution<int>(kernel);
		}

		[Test]
		public void Test_Ldc_U4()
		{
			using (var kernel = CudaKernel.Create(new Action<uint[]>(arr => arr[0] = 1)))
				VerifyExecution<uint>(kernel);
			using (var kernel = CudaKernel.Create(new Action<uint[]>(arr => arr[0] = uint.MaxValue)))
				VerifyExecution<uint>(kernel);
			using (var kernel = CudaKernel.Create(new Action<uint[]>(arr => arr[0] = uint.MinValue)))
				VerifyExecution<uint>(kernel);
		}

		[Test]
		public void Test_Blt_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Blt_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Blt_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 < arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Ble_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Ble_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Ble_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 <= arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Beq_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 == arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Beq_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 == arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Beq_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 == arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bne_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 != arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Bne_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 != arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bne_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 != arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bgt_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 > arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Bgt_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 > arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bgt_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 > arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bge_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 >= arg2 ? arg1 : arg2);
		}

		[Test]
		public void Test_Bge_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 >= arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Bge_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 >= arg2 ? arg1 : arg2, false);
		}

		[Test]
		public void Test_Add_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 + arg2);
		}

		[Test]
		public void Test_Add_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 + arg2, false);
		}

		[Test]
		public void Test_Add_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 + arg2, false);
		}

		[Test]
		public void Test_Sub_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 - arg2);
		}

		[Test]
		public void Test_Sub_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 - arg2, false);
		}

		[Test]
		public void Test_Sub_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 - arg2, false);
		}

		[Test]
		public void Test_Mul_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 * arg2);
		}

		[Test]
		public void Test_Mul_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 * arg2, false);
		}

		[Test]
		public void Test_Mul_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 * arg2, false);
		}

		[Test]
		public void Test_Div_F4()
		{
			VerifyExecution_Binary_F4((arr, arg1, arg2) => arr[0] = arg1 / arg2);
		}

		[Test]
		public void Test_Div_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 / arg2, false);
		}

		[Test]
		public void Test_Div_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 / arg2, false);
		}

		private void VerifyExecution_Binary_F4(Action<float[], float, float> del)
		{
			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution(kernel, 3.5f, 3.6f);
				VerifyExecution(kernel, 3.6f, 3.5f);
				VerifyExecution(kernel, 3.5f, -3.6f);
				VerifyExecution(kernel, -3.6f, 3.5f);

				VerifyExecution(kernel, float.PositiveInfinity, 3.5f);
				VerifyExecution(kernel, 3.5f, float.PositiveInfinity);
				VerifyExecution(kernel, float.NegativeInfinity, 3.5f);
				VerifyExecution(kernel, 3.5f, float.NegativeInfinity);

				VerifyExecution(kernel, float.NaN, 3.5f);
				VerifyExecution(kernel, 3.5f, float.NaN);

				VerifyExecution(kernel, float.PositiveInfinity, float.PositiveInfinity);
				VerifyExecution(kernel, float.NegativeInfinity, float.NegativeInfinity);
				VerifyExecution(kernel, float.PositiveInfinity, float.NegativeInfinity);
				VerifyExecution(kernel, float.NegativeInfinity, float.NegativeInfinity);

				VerifyExecution(kernel, float.PositiveInfinity, float.NaN);
				VerifyExecution(kernel, float.NaN, float.PositiveInfinity);
				VerifyExecution(kernel, float.NegativeInfinity, float.NaN);
				VerifyExecution(kernel, float.NaN, float.NegativeInfinity);
			}
		}

		private void VerifyExecution_Binary_I4(Action<int[], int, int> del, bool testBadArguments)
		{
			using (var kernel = CudaKernel.Create(del))
			{
				if (!testBadArguments)
				{
					VerifyExecution(kernel, 3, 4);
					VerifyExecution(kernel, 4, 3);
					VerifyExecution(kernel, -3, 4);
					VerifyExecution(kernel, 4, -3);
				}		
				else
				{
					// the ones that can cause exceptions.
					VerifyExecution(kernel, 4, -1);
					VerifyExecution(kernel, int.MinValue, -1);
					VerifyExecution(kernel, 4, 0);
				}
			}
		}

		private void VerifyExecution_Binary_U4(Action<uint[], uint, uint> del, bool testBadArguments)
		{
			using (var kernel = CudaKernel.Create(del))
			{
				if (!testBadArguments)
				{
					VerifyExecution(kernel, 3u, 4u);
					VerifyExecution(kernel, 4u, 3u);
					VerifyExecution(kernel, 3u, uint.MaxValue);
					VerifyExecution(kernel, uint.MaxValue, 3u);
				}
				else
				{
					// the ones that can cause exceptions.
					VerifyExecution(kernel, 4u, uint.MaxValue);
					VerifyExecution(kernel, uint.MinValue, uint.MaxValue);
					VerifyExecution(kernel, 4u, 0u);
				}
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



		void VerifyExecution<T>(CudaKernel kernel) where T : struct
		{
			if (!CudaDevice.HasCudaDevice)
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				return;
			}

			var devmem = kernel.Context.AllocateLinear<T>(16);

			kernel.SetBlockShape(1, 1);
			kernel.SetGridSize(1, 1);
			kernel.ExecuteUntyped(devmem);

			var arr = new T[devmem.Length];
			kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

			// Invoke to got correct result.
			var refExec = new T[devmem.Length];
			kernel.KernelMethod.Invoke(null, new object[] {refExec});
			var expectedResult = refExec[0];

			if (!arr[0].Equals(expectedResult))
			{
				Console.WriteLine("Failing PTX: " + kernel.GetPtx());
			}
			AreEqual(expectedResult, arr[0]);
		}
	}
}

using System;
using System.Collections.Generic;
using CellDotNet.Intermediate;
using NUnit.Framework;
using System.Linq;

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
		public void Test_Brtrue()
		{
			Action<int[], int> del = (arr, i) => { if (i == 0) arr[0] = 100; };
			using (var kernel = CudaKernel.Create(del))
			{
				kernel.PerformProcessing(CudaKernelCompileState.IRConstructionDone);
				ListInstruction brtrue = kernel.Methods.Single().Blocks
					.SelectMany(b => b.Instructions)
					.SingleOrDefault(inst => inst.IRCode == IRCode.Brtrue);
				IsNotNull(brtrue, "Did not find brtrue instruction to test.");
				VerifyExecution<int>(kernel, 0);
				VerifyExecution<int>(kernel, 1);
			}
		}

		[Test]
		public void Test_Brfalse()
		{
			Action<int[], int> del = (arr, i) => { if (i != 0) arr[0] = 100; };
			using (var kernel = CudaKernel.Create(del))
			{
				kernel.PerformProcessing(CudaKernelCompileState.IRConstructionDone);
				ListInstruction brtrue = kernel.Methods.Single().Blocks
					.SelectMany(b => b.Instructions)
					.SingleOrDefault(inst => inst.IRCode == IRCode.Brfalse);
				IsNotNull(brtrue, "Did not find bfalse instruction to test.");
				VerifyExecution<int>(kernel, 0);
				VerifyExecution<int>(kernel, 1);
			}
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

		[Test]
		public void Test_And_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 & arg2, true);
		}

		[Test]
		public void Test_And_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 & arg2, true);
		}

		[Test]
		public void Test_Or_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 | arg2, true);
		}

		[Test]
		public void Test_Or_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 | arg2, true);
		}
		[Test]
		public void Test_Xor_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 ^ arg2, true);
		}

		[Test]
		public void Test_Xor_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 ^ arg2, true);
		}

		[Test]
		public void Test_Shl_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 << arg2, true);
		}

		[Test]
		public void Test_Shl_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 << (int)arg2, true);
		}

		[Test]
		public void Test_Shr_I4()
		{
			VerifyExecution_Binary_I4((arr, arg1, arg2) => arr[0] = arg1 >> arg2, true);
		}

		[Test]
		public void Test_Shr_U4()
		{
			VerifyExecution_Binary_U4((arr, arg1, arg2) => arr[0] = arg1 >> (int)arg2, true);
		}

		[Test]
		public void Test_Conv_I4_R4()
		{
			Action<int[], float> del = (arr, arg) => arr[0] = (int) arg;
			using (var kernel = CudaKernel.Create(del))
			{
				// Don't test for overflow and NaN results, because the results aren't defined, and in practice they
				// deviate from .net.
				VerifyExecution<int>(kernel, 0f);
				VerifyExecution<int>(kernel, 1f);
				VerifyExecution<int>(kernel, -1f);
				VerifyExecution<int>(kernel, 1.1f);
				VerifyExecution<int>(kernel, 1.5f);
				VerifyExecution<int>(kernel, 1.6f);
				VerifyExecution<int>(kernel, 2.1f);
				VerifyExecution<int>(kernel, 2.5f);
				VerifyExecution<int>(kernel, 2.6f);
				VerifyExecution<int>(kernel, 3f);
				VerifyExecution<uint>(kernel, 1345243.3634f);

				VerifyExecution<int>(kernel, -1.1f);
				VerifyExecution<int>(kernel, -1.5f);
				VerifyExecution<int>(kernel, -1.6f);
				VerifyExecution<int>(kernel, -2.1f);
				VerifyExecution<int>(kernel, -2.5f);
				VerifyExecution<int>(kernel, -2.6f);
				VerifyExecution<int>(kernel, -3f);
			}
		}

		[Test]
		public void Test_Conv_U4_R4()
		{
			Action<uint[], float> del = (arr, arg) => arr[0] = (uint) arg;
			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution<uint>(kernel, 0f);
				VerifyExecution<uint>(kernel, 1f);
				VerifyExecution<uint>(kernel, 1.1f);
				VerifyExecution<uint>(kernel, 1.5f);
				VerifyExecution<uint>(kernel, 1.6f);
				VerifyExecution<uint>(kernel, 2.1f);
				VerifyExecution<uint>(kernel, 2.5f);
				VerifyExecution<uint>(kernel, 2.6f);
				VerifyExecution<uint>(kernel, 3f);
				VerifyExecution<uint>(kernel, 1345243.3634f);
			}
		}

		[Test]
		public void Test_Conv_R4_I4()
		{
			Action<float[], int> del = (arr, arg) => arr[0] = arg;
			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution<float>(kernel, 0);
				VerifyExecution<float>(kernel, 1);
				VerifyExecution<float>(kernel, 10);
				VerifyExecution<float>(kernel, 200000);
				VerifyExecution<float>(kernel, int.MaxValue);
				VerifyExecution<float>(kernel, -1);
				VerifyExecution<float>(kernel, -10);
				VerifyExecution<float>(kernel, -200000);
				VerifyExecution<float>(kernel, int.MinValue);
			}
		}

		[Test]
		public void Test_Conv_R4_U4_Unsupported()
		{
			Action<float[], uint> del = (arr, arg) => arr[0] = arg;
			using (var kernel = CudaKernel.Create(del))
			{
				VerifyExecution<float>(kernel, 0u);
				VerifyExecution<float>(kernel, 1u);
				VerifyExecution<float>(kernel, 10u);
				VerifyExecution<float>(kernel, 200000u);
				VerifyExecution<float>(kernel, uint.MaxValue);
			}
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
				VerifyExecution(kernel, 3u, 4u);
				VerifyExecution(kernel, 4u, 3u);
				VerifyExecution(kernel, 3u, uint.MaxValue);
				VerifyExecution(kernel, uint.MaxValue, 3u);

				if (testBadArguments)
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
			VerifyExecution<T>(kernel, null);
		}

		void VerifyExecution<T>(CudaKernel kernel, object optionalArgument) where T : struct
		{
			if (!CudaDevice.HasCudaDevice)
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				return;
			}

			var devmem = kernel.Context.AllocateLinear<T>(16);

			kernel.SetBlockShape(1, 1);
			kernel.SetGridSize(1, 1);
			if (optionalArgument != null)
				kernel.ExecuteUntyped(devmem, optionalArgument);
			else
				kernel.ExecuteUntyped(devmem);

			var arr = new T[devmem.Length];
			kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

			// Invoke to got correct result.
			var refExec = new T[devmem.Length];
			if (optionalArgument != null)
				kernel.KernelMethod.Invoke(null, new object[] {refExec, optionalArgument});
			else
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

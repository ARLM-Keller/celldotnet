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
			var kernel = CudaKernel.Create(del.Method);
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
		public void TestIR_MultipleMethods2()
		{
			Action<int> del = delegate(int i)
			                  	{
			                  		TestIR_MultipleMethods_CallMethod(i);
			                  		TestIR_MultipleMethods_CallMethod(i);
			                  	};
			var kernel = CudaKernel.Create(del.Method);
			kernel.PerformProcessing(CudaKernelCompileState.IRConstructionDone);
			AreEqual(2, kernel.Methods.Count);
		}


		[Test]
		public void TestApiFeel()
		{
			Action<float[], int> del = delegate(float[] arr, int i) { arr[0] = 3.4f; };

			//			var kernel = CudaKernel.Create<Action<GlobalMemory<float>, int>>(del.Method);
			using (var kernel = CudaKernel.Create(del.Method))
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				Console.WriteLine(kernel.GetPtx());
				GlobalMemory<float> mem = kernel.Context.AllocateLinear<float>(200);

				kernel.SetBlockSize(32, 8);
				kernel.SetGridSize(10, 10);
				kernel.ExecuteUntyped(mem, 1);
			}
		}

		[Test]
		public void Test1x1Block()
		{
			Action<float[], int> del = delegate(float[] arr, int i) { arr[0] = 3.4f; };

			using (var kernel = CudaKernel.Create(del.Method))
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				GlobalMemory<float> mem = kernel.Context.AllocateLinear<float>(200);

				kernel.SetBlockSize(1, 1);
				kernel.SetGridSize(11, 1);
				kernel.ExecuteUntyped(mem, 1);
			}
		}

		[Test]
		public void TestLoadPtx()
		{
			var ptx = @"
	.version 1.2
	.target sm_10, map_f64_to_f32


	.entry f1
	{
	.param .s32 arr;
	.reg .s32 c1, c2;
	.reg .s32 diff;
	.reg .s32 o1, o2;
	.reg .u32 parr;
	
	mov.s32 c1, %clock;
	mov.s32 c2, %clock;
	ld.param.u32 parr, [arr];
	st.global.s32 [parr], 123;

	ret;
	}
";
			var cubin = new PtxCompiler().CompileToCubin(ptx);
			using (var kernel = CudaKernel.FromCubin(cubin))
			{
				kernel.SetBlockSize(1);
				kernel.SetGridSize(1);

				var mem = kernel.Context.AllocateLinear<int>(16);
				kernel.ExecuteUntyped(mem);
				var arr = kernel.Context.CopyDeviceToHost(mem, 0, mem.Length);

				AreEqual(123, arr[0]);
//				IsTrue(arr[0] >= 4 || arr[0] <= 30, "Unexpected %clock. Timing 1: " + arr[1] + "; timing 2: " + arr[2]);
			}
		}

	}
}

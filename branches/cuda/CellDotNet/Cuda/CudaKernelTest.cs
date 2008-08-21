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
		public void TestApiFeel()
		{
			Action<float[], int> del = delegate(float[] arr, int i) { arr[0] = 3.4f; };

			//			var kernel = CudaKernel.Create<Action<GlobalMemory<float>, int>>(del.Method);
			using (var kernel = CudaKernel.Create(del.Method))
			{
				kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
				Console.WriteLine(kernel.GetPtx());
				GlobalMemory<float> mem = kernel.Context.AllocateLinear<float>(200);

				kernel.SetBlockShape(32, 8);
				kernel.SetGridSize(10, 10);
				kernel.ExecuteUntyped(mem, 1);
			}
		}
	}
}

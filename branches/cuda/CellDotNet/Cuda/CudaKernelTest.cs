using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			var kernel = new CudaKernel<Action<int>>(del);
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
	}
}

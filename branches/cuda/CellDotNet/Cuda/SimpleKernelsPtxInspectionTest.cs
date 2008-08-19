using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class SimpleKernelsPtxInspectionTest
	{
		[Test]
		public void TestMethodDeclaration_IntInt()
		{
			Action<int, int> del = (a, b) => { };
			DumpPtx(del.Method);
		}
			
		[Test]
		public void TestMethodDeclaration_IntIntArray()
		{
			Action<int, int[]> del = (i, arr) => { };
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgumentAssignment()
		{
			Action<int, int, int[]> del = (i, val, arr) => { arr[i] = val; };
			DumpPtx(del.Method);
		}

		private void DumpPtx(MethodInfo method)
		{
			CudaMethod cm = new CudaMethod(method);
			cm.PerformProcessing(CudaMethodCompileState.InstructionSelectionDone);
			var emitter = new PtxEmitter();
			emitter.Emit(cm);
			Console.WriteLine(emitter.GetEmittedPtx());
		}
	}
}

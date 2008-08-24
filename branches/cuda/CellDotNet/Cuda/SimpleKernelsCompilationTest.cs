using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class SimpleKernelsCompilationTest : UnitTest
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
		public void TestGlobalArrayArgument_StoreInt()
		{
			Action<int, int, int[]> del = (i, val, arr) => { arr[i] = val; };
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgument_StoreFloat()
		{
			Action<int, float, float[]> del = (i, val, arr) => { arr[i] = val; };
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgument_LoadStoreInt()
		{
			Action<int, int, int[]> del = (i, val, arr) => { arr[i] = arr[i + 1]; };
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgument_LoadStoreFloat()
		{
			Action<int, float, float[]> del = (i, val, arr) => { arr[i] = arr[i+1]; };
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgument_LoadStoreInt_Copy()
		{
			// Check that we avoid repeated argument loads.
			Action<int, int, int[]> del = (i, val, arr) =>
			                              	{
			                              		var arr2 = arr;
			                              		var i2 = i;
			                              		arr2[i2] = arr2[i2 + 1];
			                              	};
			DumpPtx(del.Method);
		}

		[Test]
		public void TestGlobalArrayArgument_LoadStoreFloat_Copy()
		{
			// Check that we avoid repeated argument loads.
			Action<int, float, float[]> del = (i, val, arr) =>
			                                  	{
													var arr2 = arr;
													var i2 = i;
													arr2[i2] = arr2[i2 + 1];
												};
			DumpPtx(del.Method);
		}

		private void DumpPtx(MethodInfo method)
		{
			// First avoid CudaKernel.
			var cm = new CudaMethod(method);
			cm.PerformProcessing(CudaMethodCompileState.InstructionSelectionDone);
			var emitter = new PtxEmitter();
			emitter.Emit(cm);
			string ptx = emitter.GetEmittedPtx();
			Console.WriteLine(ptx);
			string cubin = new PtxCompiler().CompileToCubin(ptx);
			Console.WriteLine();
			Console.WriteLine("Cubin:");
			Console.WriteLine(cubin);

			// .. then try the whole thing.
			using (var kernel = CudaKernel.Create(method))
			{
				if (CudaDevice.HasCudaDevice)
					kernel.EnsurePrepared();
				else
					kernel.PerformProcessing(CudaKernelCompileState.Complete);
			}
		}
	}
}

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

		[Test]
		public void TestArrayStoreMemoryCopy()
		{
			Action<int, float, float[]> del = (index, value, arr) =>
			                                  	{
			                                  		arr[index] = value;
			                                  	};
			using (var kernel = CudaKernel.Create(del))
			{
				const int index = 5;
				const float value = 50f;

				var devmem = kernel.Context.AllocateLinear<float>(10);

				kernel.SetBlockShape(16, 16);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped(index, value, devmem);

				var arr = new float[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

				AreEqual(value, arr[index]);
			}
		}

		[Test]
		public void TestThreadIndexX()
		{
			Action<int[]> del = arr => arr[ThreadIndex.X] = ThreadIndex.X;

			using (var kernel = CudaKernel.Create(del))
			{
				if (!CudaDevice.HasCudaDevice)
				{
					kernel.PerformProcessing(CudaKernelCompileState.PtxEmissionComplete);
					return;
				}

				var devmem = kernel.Context.AllocateLinear<int>(16);

				kernel.SetBlockShape(16, 1);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped(devmem);

				var arr = new int[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

				var arrCorrect = new int[devmem.Length];
				for (int x = 0; x < 16; x++)
					arrCorrect[x] = x;

				AreEqual(arrCorrect, arr);
			}
		}

		[Test]
		public void TestThreadIndexXY()
		{
			Action<int[]> del = arr =>
			{
				arr[ThreadIndex.X + (ThreadIndex.Y * BlockSize.X)] = ThreadIndex.X + (ThreadIndex.Y * BlockSize.X);
			};
			using (var kernel = CudaKernel.Create(del))
			{
				const int blockSizeX = 16;
				const int blockSizeY = 16;

				var devmem = kernel.Context.AllocateLinear<int>(blockSizeX * blockSizeY);
				kernel.SetBlockShape(blockSizeX, blockSizeY);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped(devmem);

				var arr = new int[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

				var arrCorrect = new int[devmem.Length];
				for (int x = 0; x < blockSizeX; x++)
					for (int y = 0; y < blockSizeY; y++)
						arrCorrect[x + y * blockSizeX] = x + y * blockSizeX;

				AreEqual(arrCorrect, arr);
			}
		}

		[Test]
		public void TestThreadIndexXYZ()
		{
			Action<int[]> del = arr =>
			{
				int val = ThreadIndex.X + (ThreadIndex.Y*BlockSize.X) + (ThreadIndex.Z*BlockSize.X*BlockSize.Y);
				arr[val] = val;
			};
			using (var kernel = CudaKernel.Create(del))
			{
				const int blockSizeX = 16;
				const int blockSizeY = 16;
				const int blockSizeZ = 2;

				var devmem = kernel.Context.AllocateLinear<int>(blockSizeX * blockSizeY * blockSizeZ);
				kernel.SetBlockShape(blockSizeX, blockSizeY, blockSizeZ);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped(devmem);

				var arr = new int[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

				var arrCorrect = new int[devmem.Length];
				for (int x = 0; x < blockSizeX; x++)
					for (int y = 0; y < blockSizeY; y++)
						for (int z = 0; z < blockSizeZ; z++)
							arrCorrect[x + y * blockSizeX + z * blockSizeX * blockSizeY] = x + y * blockSizeX + z * blockSizeX * blockSizeY;

				AreEqual(arrCorrect, arr);
			}
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

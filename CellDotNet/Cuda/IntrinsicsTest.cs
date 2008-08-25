using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class IntrinsicsTest : UnitTest
	{
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
				int val = ThreadIndex.X + (ThreadIndex.Y * BlockSize.X) + (ThreadIndex.Z * BlockSize.X * BlockSize.Y);
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

		[Test]
		public void CheckBlockIndexZY()
		{
			Action<int[]> del = arr =>
			{
				int val = BlockIndex.X + (BlockIndex.Y * GridSize.X);
				arr[val] = val;
			};
			using (var kernel = CudaKernel.Create(del))
			{
				const int gridSizeX = 2;
				const int gridSizeY = 2;

				var devmem = kernel.Context.AllocateLinear<int>(gridSizeX * gridSizeY);
				kernel.SetBlockShape(1, 1);
				kernel.SetGridSize(gridSizeX, gridSizeY);
				kernel.ExecuteUntyped(devmem);

				var arr = new int[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, arr, 0, arr.Length);

				var arrCorrect = new int[devmem.Length];
				for (int x = 0; x < gridSizeX; x++)
					for (int y = 0; y < gridSizeY; y++)
						arrCorrect[x + y * gridSizeX] = x + y * gridSizeX;

				AreEqual(arrCorrect, arr);
			}
		}

		[Test]
		public void TestSyncThreads()
		{
			// Should probably make a better test to actually detect the effect of syncthreads.
			Action<int[]> del = arr =>
			                    	{
			                    		arr[0] = 5;
			                    		CudaRuntime.SyncThreads();
										arr[0] = 2;
									};
			using (var kernel = CudaKernel.Create(del))
			{
				kernel.SetBlockShape(16, 16);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped();
				bool hasBar = kernel.GetPtx().IndexOf("bar.sync") != -1;
				IsTrue(hasBar);
			}
		}

		[Test]
		public void TestClock()
		{
			Action<int[]> del = arr =>
			                    	{
			                    		int c1 = CudaRuntime.GetClock();
			                    		int c2 = CudaRuntime.GetClock();
			                    		int cyclecount = c2 - c1;
			                    		arr[0] = cyclecount;
										arr[1] = c1;
										arr[2] = c2;
									};
			using (var kernel = CudaKernel.Create(del))
			{
				kernel.PerformProcessing(CudaKernelCompileState.Complete);
				bool hasClock = kernel.GetPtx().IndexOf("%clock") != -1;
				IsTrue(hasClock);

				var mem = kernel.Context.AllocateLinear<int>(16);
				kernel.SetBlockShape(1, 1);
				kernel.SetGridSize(1, 1);
				kernel.ExecuteUntyped(mem);
				var arr = kernel.Context.CopyDeviceToHost(mem, 0, 16);
				IsTrue(arr[0] == 4 || arr[0] == 8, "Unexpected %clock. Timing 1: " + arr[1] + "; timing 2: " + arr[2] + "; ptx:\r\n" + kernel.GetPtx());
			}
		}

		[Test]
		public void TestClock_Manual()
		{
			var ptx =@"
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

	add.u32 o1, parr, 4;
	st.global.s32 [o1], c1;

	add.u32 o2, parr, 8;
	st.global.s32 [o2], c2;
	
	sub.s32 diff, c2, c1;
	st.global.s32 [parr], diff;

	
	ret;
	}

";
			var cubin = new PtxCompiler().CompileToCubin(ptx);
			using (var ctx = CudaContext.GetCurrentOrNew())
			{
				var module = CudaModule.LoadData(cubin);
				CudaFunction function = module.GetFunction("f1");
				function.SetBlockSize(1, 1, 1);
				function.SetGridSize(1, 1);

				var mem = ctx.AllocateLinear<int>(16);
				function.Launch(new object[] { mem });
				var arr = ctx.CopyDeviceToHost(mem, 0, mem.Length);

				IsTrue(arr[0] == 4 || arr[0] == 8, "Unexpected %clock. Timing 1: " + arr[1] + "; timing 2: " + arr[2]);
			}
		}
	}
}

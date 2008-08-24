﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class MemoryTest : UnitTest
	{
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

		[StaticArray(2)]
		private static Shared1D<int> sharedbuffer;

		[Test]
		public void TestSharedMemory()
		{
			// Should also test use of same buffer from different methods.
			Action<int[]> del = arr =>
			{
				sharedbuffer[ThreadIndex.X] = ThreadIndex.X + 1;
				CudaRuntime.SyncThreads();
				arr[0] = sharedbuffer[0] + sharedbuffer[1];
			};
			using (var kernel = CudaKernel.Create(del))
			{
				kernel.PerformProcessing(CudaKernelCompileState.Complete);
				IsTrue(kernel.GetPtx().IndexOf("ld.shared") != -1);
				IsTrue(kernel.GetPtx().IndexOf("st.shared") != -1);

				var devmem = kernel.Context.AllocateLinear<int>(16);
				kernel.SetGridSize(1, 1);
				kernel.SetBlockShape(2, 1);
				kernel.ExecuteUntyped(devmem);
				var ret = new int[devmem.Length];
				kernel.Context.CopyDeviceToHost(devmem, 0, ret, 0, devmem.Length);

				AreEqual(3, ret[0]);
			}
		}
	}
}
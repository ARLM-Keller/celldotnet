using System;
using System.Collections.Generic;

namespace CellDotNet.Cuda.Samples
{
	internal class ClockSample
	{
		private const int NUM_BLOCKS = 64;
		private const int NUM_THREADS = 256;

		[StaticArray(NUM_THREADS*2)]
		private static Shared1D<float> shared;

		/// This kernel computes a standard parallel reduction and evaluates the
		/// time it takes to do that for each block. The timing results are stored 
		/// in device memory.
		/// static void timedReduction(const float * input, float * output, clock_t * timer)
		private static void TimedReduction(float[] input, float[] output, int[] timer)
		{
			int tid = ThreadIndex.X;
			int bid = BlockIndex.X;

			if (tid == 0) timer[bid] = CudaRuntime.GetClock();

			// Copy input.
			shared[tid] = input[tid];
			shared[tid + BlockSize.X] = input[tid + BlockSize.X];

			// Perform reduction to find minimum.
			for (int d = BlockSize.X; d > 0; d /= 2)
			{
				CudaRuntime.SyncThreads();

				if (tid < d)
				{
					float f0 = shared[tid];
					float f1 = shared[tid + d];

					if (f1 < f0)
					{
						shared[tid] = f1;
					}
				}
			}

			// Write result.
			if (tid == 0) output[bid] = shared[0];

			CudaRuntime.SyncThreads();

			if (tid == 0) timer[bid + GridSize.X] = CudaRuntime.GetClock();
		}

		public static void Run()
		{
			var del = new Action<float[], float[], int[]>(TimedReduction);
			using (var kernel = CudaKernel.Create(del))
			{
				var input = new float[NUM_THREADS * 2];
				for (int i = 0; i < NUM_THREADS * 2; i++)
				{
					input[i] = i;
				}

				GlobalMemory<float> inputmem = kernel.Context.AllocateLinear<float>(input.Length);
				GlobalMemory<float> outputmem = kernel.Context.AllocateLinear<float>(NUM_BLOCKS);
				GlobalMemory<int> timermem = kernel.Context.AllocateLinear<int>(NUM_BLOCKS*2);

				kernel.Context.CopyHostToDevice(input, 0, inputmem, 0, input.Length);
				kernel.SetBlockSize(NUM_THREADS);
				kernel.SetGridSize(NUM_BLOCKS);

				kernel.ExecuteUntyped(inputmem, outputmem, timermem);
				int[] timer = kernel.Context.CopyDeviceToHost(timermem, 0, timermem.Length);

				// This test always passes.
				Console.WriteLine("Test PASSED\n");

				// Compute the difference between the last block end and the first block start.
				int minStart = timer[0];
				int maxEnd = timer[NUM_BLOCKS];

				for (int i = 1; i < NUM_BLOCKS; i++)
				{
					minStart = timer[i] < minStart ? timer[i] : minStart;
					maxEnd = timer[NUM_BLOCKS + i] > maxEnd ? timer[NUM_BLOCKS + i] : maxEnd;
				}

				Console.WriteLine("Time = {0}", maxEnd - minStart);
			}
		}
	}
}

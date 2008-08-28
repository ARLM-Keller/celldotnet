using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace CellDotNet.Cuda.Samples
{
	internal class TransposeSample
	{
		private const int BLOCK_DIM = 16;

		[StaticArray(BLOCK_DIM*(BLOCK_DIM + 1))] private static Shared1D<float> block;

		/// <summary>
		/// This kernel is optimized to ensure all global reads and writes are coalesced,
		/// and to avoid bank conflicts in shared memory.  This kernel is up to 11x faster
		/// than the naive kernel below.  Note that the shared memory array is sized to 
		/// (BLOCK_DIM+1)*BLOCK_DIM.  This pads each row of the 2D block in shared memory 
		/// so that bank conflicts do not occur when threads address the array column-wise.
		/// </summary>
		static private void Transpose(float[] odata, float[] idata, int width, int height)
		{
			// read the matrix tile into shared memory
			int xIndex = BlockIndex.X*BLOCK_DIM + ThreadIndex.X;
			int yIndex = BlockIndex.Y*BLOCK_DIM + ThreadIndex.Y;
			if ((xIndex < width) && (yIndex < height))
			{
				int index_in = yIndex*width + xIndex;
				block[ThreadIndex.Y*(BLOCK_DIM + 1) + ThreadIndex.X] = idata[index_in];
			}

			CudaRuntime.SyncThreads();

			// write the transposed matrix tile to global memory
			xIndex = BlockIndex.Y*BLOCK_DIM + ThreadIndex.X;
			yIndex = BlockIndex.X*BLOCK_DIM + ThreadIndex.Y;
			if ((xIndex < height) && (yIndex < width))
			{
				int index_out = yIndex*height + xIndex;
				odata[index_out] = block[ThreadIndex.X*(BLOCK_DIM + 1) + ThreadIndex.Y];
			}
		}


		/// <summary>
		/// This naive transpose kernel suffers from completely non-coalesced writes.
		/// It can be up to 10x slower than the kernel above for large matrices.
		/// </summary>
		static private void TransposeNaive(float[] odata, float[] idata, int width, int height)
		{
			int xIndex = BlockSize.X*BlockIndex.X + ThreadIndex.X;
			int yIndex = BlockSize.Y*BlockIndex.Y + ThreadIndex.Y;

			if (xIndex < width && yIndex < height)
			{
				int index_in = xIndex + width*yIndex;
				int index_out = yIndex + height*xIndex;
				odata[index_out] = idata[index_in];
			}
		}

		/// <summary>
		/// Compute reference data set.
		/// </summary>
		private void ComputeGold(float[] reference, float[] idata, int size_x, int size_y)
		{
			// transpose matrix
			for (int y = 0; y < size_y; ++y)
			{
				for (int x = 0; x < size_x; ++x)
				{
					reference[(x*size_y) + y] = idata[(y*size_x) + x];
				}
			}
		}

		public void RunTest()
		{
			// size of the matrix
#if __DEVICE_EMULATION__
    int size_x = 32;
    int size_y = 128;
#else
			int size_x = 256;
			int size_y = 4096;
#endif

			// size of memory required to store the matrix
			int elementCount = size_x*size_y;

			// allocate host memory
			var h_idata = new float[elementCount];
			// initalize the memory
			var r = new Random(15235911);
			for (int i = 0; i < (size_x*size_y); ++i)
			{
				h_idata[i] = i; // rand(); 
			}

			using (CudaKernel trans = CudaKernel.Create(new Action<float[], float[], int, int>(Transpose)))
			using (CudaKernel transNaive = CudaKernel.Create(new Action<float[], float[], int, int>(TransposeNaive)))
			{
				// allocate device memory
				GlobalMemory<float> d_idata = trans.Context.AllocateLinear<float>(elementCount);
				GlobalMemory<float> d_odata = trans.Context.AllocateLinear<float>(elementCount);

				// copy host memory to device
				trans.Context.CopyHostToDevice(h_idata, 0, d_idata, 0, h_idata.Length);

				// setup execution parameters
				trans.SetGridSize(size_x/BLOCK_DIM, size_y/BLOCK_DIM);
				transNaive.SetGridSize(size_x/BLOCK_DIM, size_y/BLOCK_DIM);
				trans.SetBlockSize(BLOCK_DIM, BLOCK_DIM);
				transNaive.SetBlockSize(BLOCK_DIM, BLOCK_DIM);

				// warmup so we don't time CUDA startup
				transNaive.ExecuteUntyped(d_odata, d_idata, size_x, size_y);
				trans.ExecuteUntyped(d_odata, d_idata, size_x, size_y);

				const int numIterations = 1;

				Console.WriteLine("Transposing a {0} by {1} matrix of floats...", size_x, size_y);

				var timer = new Stopwatch();
				// execute the kernel
				timer.Start();
				for (int i = 0; i < numIterations; ++i)
				{
					transNaive.ExecuteUntyped(d_odata, d_idata, size_x, size_y);
				}
				trans.Context.Synchronize();
				timer.Stop();
				var naiveTime = (float) timer.Elapsed.TotalMilliseconds;
				timer.Reset();

				// execute the kernel

				timer.Start();
				for (int i = 0; i < numIterations; ++i)
				{
					trans.ExecuteUntyped(d_odata, d_idata, size_x, size_y);
				}
				trans.Context.Synchronize();
				timer.Stop();
				var optimizedTime = (float)timer.Elapsed.TotalMilliseconds;

				Console.WriteLine("Naive transpose average time:     {0:F03} ms", naiveTime/numIterations);
				Console.WriteLine("Optimized transpose average time: {0:F03} ms", optimizedTime/numIterations);
				Console.WriteLine();

				// copy result from device to host
				var h_odata = new float[elementCount];
				trans.Context.CopyDeviceToHost(d_odata, 0, h_odata, 0, h_odata.Length);

				// compute reference solution
				var reference = new float[elementCount];
				ComputeGold(reference, h_idata, size_x, size_y);

				// check result
				bool res = cutComparef(reference, h_odata, size_x*size_y);
				Console.WriteLine("Test {0}", res ? "PASSED" : "FAILED");

				// cleanup memory
				d_idata.Free();
				d_odata.Free();
			}
		}

		private bool cutComparef(float[] reference, float[] data, int len)
		{
			float epsilon = 0.0f;
			return compareData(reference, data, len, epsilon);
		}

		private bool compareData(float[] reference, float[] data, int len, float epsilon)
		{
			Utilities.AssertArgumentRange(epsilon >= 0, "epsilon", epsilon);

			bool result = true;
			for (int i = 0; i < len; ++i)
			{
				float diff = reference[i] - data[i];
				bool comp = (diff <= epsilon) && (diff >= -epsilon);
				result &= comp;

				if (!comp)
				{
					Console.WriteLine("ERROR, i = " + i + ",\t "
					                  + reference[i] + " / "
					                  + data[i]
					                  + " (reference / data)");
				}
			}

			return result;
		}
	}
}

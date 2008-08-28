using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace CellDotNet.Cuda.Samples
{
	internal class MatrixMulSample
	{
		private const int BLOCK_SIZE = 16;
		// Matrix dimensions
		// (chosen as multiples of the thread block size for simplicity)
		const int WA = (3 * BLOCK_SIZE); // Matrix A width
		const int HA = (5 * BLOCK_SIZE); // Matrix A height
		const int WB = (8 * BLOCK_SIZE); // Matrix B width
		const int HB = WA;  // Matrix B height
		const int WC = WB;  // Matrix C width 
		const int HC = HA;  // Matrix C height

		/// <summary>
		/// Declaration of the shared memory array As used to store the sub-matrix of A.
		/// </summary>
		[StaticArray(BLOCK_SIZE * BLOCK_SIZE)]
		private static Shared1D<float> As;

		/// <summary>
		/// Declaration of the shared memory array Bs used to store the sub-matrix of B.
		/// </summary>
		[StaticArray(BLOCK_SIZE * BLOCK_SIZE)]
		private static Shared1D<float> Bs;

		/// <summary>
		/// The kernel.
		/// </summary>
		/// <param name="C"></param>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="wA"></param>
		/// <param name="wB"></param>
		static private void MatrixMul(float[] C, float[] A, float[] B, int wA, int wB)
		{
			// Block index
			int bx = BlockIndex.X;
			int by = BlockIndex.Y;

			// Thread index
			int tx = ThreadIndex.X;
			int ty = ThreadIndex.Y;

			// Index of the first sub-matrix of A processed by the block
			int aBegin = wA * BLOCK_SIZE * by;

			// Index of the last sub-matrix of A processed by the block
			int aEnd = aBegin + wA - 1;

			// Step size used to iterate through the sub-matrices of A
			int aStep = BLOCK_SIZE;

			// Index of the first sub-matrix of B processed by the block
			int bBegin = BLOCK_SIZE * bx;

			// Step size used to iterate through the sub-matrices of B
			int bStep = BLOCK_SIZE * wB;

			// Csub is used to store the element of the block sub-matrix
			// that is computed by the thread
			float Csub = 0;

			// Loop over all the sub-matrices of A and B
			// required to compute the block sub-matrix
			for (int a = aBegin, b = bBegin;
				 a <= aEnd;
				 a += aStep, b += bStep)
			{
				// Load the matrices from device memory
				// to shared memory; each thread loads
				// one element of each matrix
				As[ty * BLOCK_SIZE + tx] = A[a + wA * ty + tx];
				// AS(ty, tx) = A[a + wA * ty + tx];
				Bs[ty * BLOCK_SIZE + tx] = B[b + wB * ty + tx];
				// BS(ty, tx) = B[b + wB * ty + tx];

				// Synchronize to make sure the matrices are loaded
				CudaRuntime.SyncThreads();

				// Multiply the two matrices together;
				// each thread computes one element
				// of the block sub-matrix
				for (int k = 0; k < BLOCK_SIZE; ++k)
					Csub += As[ty * BLOCK_SIZE + k] * Bs[k * BLOCK_SIZE + tx];
				// Csub += AS(ty, k) * BS(k, tx);

				// Synchronize to make sure that the preceding
				// computation is done before loading two new
				// sub-matrices of A and B in the next iteration
				CudaRuntime.SyncThreads();
			}

			// Write the block sub-matrix to device memory;
			// each thread writes one element
			int c = wB * BLOCK_SIZE * by + BLOCK_SIZE * bx;
			C[c + wB * ty + tx] = Csub;
		}


		/// <summary>
		/// Compute reference data set <c>C = A * B</c>.
		/// </summary>
		/// <param name="C">reference data, computed but preallocated</param>
		/// <param name="A">matrix A as provided to device</param>
		/// <param name="B">matrix B as provided to device</param>
		/// <param name="hA">height of matrix A</param>
		/// <param name="wA"></param>
		/// <param name="wB">width of matrix B</param>
		private void ComputeGold(float[] C, float[] A, float[] B, uint hA, uint wA, uint wB)
		{
			for (uint i = 0; i < hA; ++i)
				for (uint j = 0; j < wB; ++j)
				{
					double sum = 0;
					for (uint k = 0; k < wA; ++k)
					{
						double a = A[i * wA + k];
						double b = B[k * wB + j];
						sum += a * b;
					}
					C[i * wB + j] = (float)sum;
				}
		}


		Random rand;

		public void runTest()
		{
			// set seed for rand()
			rand = new Random(2006);

//			HighResolutionTimer t = new HighResolutionTimer();
//			t.Start();
//			Thread.Sleep(450);
//			t.Stop();
//			Console.WriteLine("time: " + t.Seconds);
//			return;

			// allocate host memory for matrices A and B
			int size_A = WA * HA;

			var h_A = new float[size_A];
			int size_B = WB * HB;
			var h_B = new float[size_B];

			// initialize host memory
			randomInit(h_A);
			randomInit(h_B);

			MethodInfo method = GetType().GetMethod("MatrixMul", BindingFlags.Static | BindingFlags.NonPublic);
			using (CudaKernel kernel = CudaKernel.Create(method))
			{
				// allocate device memory
				GlobalMemory<float> d_A = kernel.Context.AllocateLinear<float>(size_A);
				GlobalMemory<float> d_B = kernel.Context.AllocateLinear<float>(size_B);

				// copy host memory to device.
				kernel.Context.CopyHostToDevice(h_A, 0, d_A, 0, h_A.Length);
				kernel.Context.CopyHostToDevice(h_B, 0, d_B, 0, h_B.Length);

				// allocate device memory for result
				int size_C = WC * HC;
				GlobalMemory<float> d_C = kernel.Context.AllocateLinear<float>(size_C);

				// allocate host memory for the result
				var h_C = new float[size_C];

				// setup execution parameters
				kernel.SetBlockShape(BLOCK_SIZE, BLOCK_SIZE);
				kernel.SetGridSize(WC / BLOCK_SIZE, HC / BLOCK_SIZE);

				// create and start timer
				var timer = new HighResolutionTimer();
				timer.Start();

				// execute the kernel
				kernel.ExecuteUntyped(d_C, d_A, d_B, WA, WB);

				// copy result from device to host
				kernel.Context.CopyDeviceToHost(d_C, 0, h_C, 0, d_C.Length);

				// stop and destroy timer
				timer.Stop();
				Console.WriteLine("Processing time: {0} (ms)", timer.Seconds * 1000);

				// compute reference solution
				var reference = new float[size_C];
				ComputeGold(reference, h_A, h_B, HA, WA, WB);

				// check result
				bool res = cutCompareL2fe(reference, h_C, size_C, 1e-6f);
				Console.WriteLine("Test {0}", res ? "PASSED" : "FAILED");
				if (!res)
					printDiff(reference, h_C, WC, HC);
			}
		}

		// Allocates a matrix with random float entries.
		void randomInit(float[] data)
		{
			for (int i = 0; i < data.Length; ++i)
				data[i] = (float)rand.NextDouble();
		}

		static void printDiff(float[] data1, float[] data2, int width, int height)
		{
			int i, j, k;
			int error_count = 0;
			for (j = 0; j < height; j++)
			{
				for (i = 0; i < width; i++)
				{
					k = j * width + i;
					if (data1[k] != data2[k])
					{
						Console.WriteLine("diff({0},{1}) CPU={2}, GPU={3}", i, j, data1[k], data2[k]);
						//						Console.WriteLine("diff({0},{1}) CPU={2}, GPU=%4.4f n", i, j, data1[k], data2[k]);
						error_count++;
					}
				}
			}
			Console.WriteLine(" nTotal Errors = {0}", error_count);
		}

		/// <summary>
		/// Compare two float arrays using L2-norm with an epsilon tolerance for equality.
		/// </summary>
		/// <param name="reference">handle to the reference data / gold image</param>
		/// <param name="data">handle to the computed data</param>
		/// <param name="len">number of elements in reference and data</param>
		/// <param name="epsilon">epsilon to use for the comparison</param>
		/// <returns>
		/// true if <paramref>reference</paramref> and <paramref>data</paramref> are identical, otherwise false.
		/// </returns>
		private static bool cutCompareL2fe(float[] reference, float[] data, int len, float epsilon)
		{
			Utilities.AssertArgumentRange(epsilon >= 0, "epsilon", epsilon);

			float error = 0;
			float reff = 0;

			for (uint i = 0; i < len; ++i)
			{
				float diff = reference[i] - data[i];
				error += diff * diff;
				reff += reference[i] * reference[i];
			}

			var normRef = (float)Math.Sqrt(reff);
			if (Math.Abs(reff) < 1e-7f)
			{
#if DEBUG
				Console.WriteLine("ERROR, reference l2-norm is 0");
#endif
				return false;
			}
			var normError = (float)Math.Sqrt(error);
			error = normError / normRef;
			bool result = error < epsilon;

#if DEBUG
			if (!result)
				Console.WriteLine("ERROR, l2-norm error " + error + " is greater than epsilon " + epsilon);
#endif

			return result;
		}

	}
}

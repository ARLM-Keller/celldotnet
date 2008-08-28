using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CellDotNet.Cuda.Samples
{
	delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

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
		/// The kernel.
		/// </summary>
		/// <param name="C"></param>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="wA"></param>
		/// <param name="wB"></param>
		static private void MatrixMul_Unrolled(float[] C, float[] A, float[] B, int wA, int wB)
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
				Bs[ty * BLOCK_SIZE + tx] = B[b + wB * ty + tx];

				// Synchronize to make sure the matrices are loaded
				CudaRuntime.SyncThreads();

				// Multiply the two matrices together;
				// each thread computes one element
				// of the block sub-matrix
				int tyBlock = ty * BLOCK_SIZE;
				{
					Csub += As[tyBlock + 0] * Bs[0 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 1] * Bs[1 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 2] * Bs[2 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 3] * Bs[3 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 4] * Bs[4 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 5] * Bs[5 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 6] * Bs[6 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 7] * Bs[7 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 8] * Bs[8 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 9] * Bs[9 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 10] * Bs[10 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 11] * Bs[11 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 12] * Bs[12 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 13] * Bs[13 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 14] * Bs[14 * BLOCK_SIZE + tx];
					Csub += As[tyBlock + 15] * Bs[15 * BLOCK_SIZE + tx];
				}

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

		public void RunTest()
		{
			Action<CudaKernel, int> runmultipletests = (kernel1, count1) =>
			{
				var mslist = new List<double>();
				for (int i = 0; i < count1; i++)
				{
					double? ms = RunTest(kernel1);
					if (ms != null)
						mslist.Add(ms.Value);
				}
				mslist.Sort();
				double average = mslist.Skip(2).Take(count1 - 4).Average();
				Console.WriteLine("Average: {0:F04}", average);
			};

			const int count = 10;
			Action<float[], float[], float[], int, int> del;

			del = MatrixMul;
			using (var kernel = CudaKernel.Create(del))
			{
				Console.WriteLine();
				Console.WriteLine("Running...");
				runmultipletests(kernel, count);
			}

			del = MatrixMul_Unrolled;
			using (var kernel = CudaKernel.Create(del))
			{
				Console.WriteLine();
				Console.WriteLine("Running unrolled...");
				runmultipletests(kernel, count);
			}

			string cubin = new PtxCompiler().CompileToCubin(_nativePtx);
			using (var kernel = CudaKernel.FromCubin(cubin))
			{
				Console.WriteLine();
				Console.WriteLine("Running native...");
				runmultipletests(kernel, count);
			}
		}

		public double? RunTest(CudaKernel kernel)
		{
			// set seed for rand()
			rand = new Random(2006);

			// allocate host memory for matrices A and B
			int size_A = WA*HA;

			var h_A = new float[size_A];
			int size_B = WB*HB;
			var h_B = new float[size_B];

			// initialize host memory
			RandomInit(h_A);
			RandomInit(h_B);

			kernel.PerformProcessing(CudaKernelCompileState.Complete);
//			Debug.WriteLine(kernel.GetPtx());

			// allocate device memory
			GlobalMemory<float> d_A = kernel.Context.AllocateLinear<float>(size_A);
			GlobalMemory<float> d_B = kernel.Context.AllocateLinear<float>(size_B);

			// copy host memory to device.
			kernel.Context.CopyHostToDevice(h_A, 0, d_A, 0, h_A.Length);
			kernel.Context.CopyHostToDevice(h_B, 0, d_B, 0, h_B.Length);

			// allocate device memory for result
			int size_C = WC*HC;
			GlobalMemory<float> d_C = kernel.Context.AllocateLinear<float>(size_C);

			// allocate host memory for the result
			var h_C = new float[size_C];

			// setup execution parameters
			kernel.SetBlockSize(BLOCK_SIZE, BLOCK_SIZE);
			kernel.SetGridSize(WC/BLOCK_SIZE, HC/BLOCK_SIZE);

			// create and start timer
			var timer = new HighResolutionTimer();
			timer.Start();

			// execute the kernel
			kernel.ExecuteUntyped(d_C, d_A, d_B, WA, WB);

			// copy result from device to host
			kernel.Context.CopyDeviceToHost(d_C, 0, h_C, 0, d_C.Length);

			// stop and destroy timer
			timer.Stop();
			double ms = timer.Seconds*1000;
			Console.WriteLine("Kernele time: {0:F04} (ms)", ms);

			// compute reference solution
			var reference = new float[size_C];
			timer.Start();
			ComputeGold(reference, h_A, h_B, HA, WA, WB);
			timer.Stop();
			Console.WriteLine("Kernel time, reference solution: {0:F04} (ms)", timer.Seconds*1000);

			// check result
			bool res = CutCompareL2fe(reference, h_C, size_C, 1e-6f);
			Console.WriteLine("Test {0}", res ? "PASSED" : "FAILED");
			if (!res)
				PrintDiff(reference, h_C, WC, HC);

			d_A.Free();
			d_B.Free();
			d_C.Free();

			return res ? ms : (double?) null;
		}

		// Allocates a matrix with random float entries.
		void RandomInit(float[] data)
		{
			for (int i = 0; i < data.Length; ++i)
				data[i] = (float)rand.NextDouble();
		}

		static void PrintDiff(float[] data1, float[] data2, int width, int height)
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
		private static bool CutCompareL2fe(float[] reference, float[] data, int len, float epsilon)
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


		private string _nativePtx = @"	.version 1.2
	.target sm_10, map_f64_to_f32
	// nvopencc built on 2008-07-16

	.reg .u32 %ra<17>;
	.reg .u64 %rda<17>;
	.reg .f32 %fa<17>;
	.reg .f64 %fda<17>;
	.reg .u32 %rv<5>;
	.reg .u64 %rdv<5>;
	.reg .f32 %fv<5>;
	.reg .f64 %fdv<5>;


	//-----------------------------------------------------------
	// Options:
	//-----------------------------------------------------------
	//  Target:ptx, ISA:sm_10, Endian:little, Pointer Size:32
	//  -O3	(Optimization level)
	//  -g0	(Debug level)
	//  -m2	(Report advisories)
	//-----------------------------------------------------------


	.entry __globfunc__Z9matrixMulPfS_S_ii
	{
	.reg .u32 %r<57>;
	.reg .f32 %f<37>;
	.reg .pred %p<4>;
	.param .u32 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_C;
	.param .u32 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_A;
	.param .u32 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_B;
	.param .s32 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_wA;
	.param .s32 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_wB;
	.shared .align 4 .b8 __cuda_Bs20[1024];
	.shared .align 4 .b8 __cuda_As1044[1024];
	.loc	14	60	0
$LBB1___globfunc__Z9matrixMulPfS_S_ii:
	.loc	14	91	0
	cvt.s32.u16 	%r1, %ctaid.x;   	// 
	mul24.lo.s32 	%r2, %r1, 16;   	// 
	cvt.s32.u16 	%r3, %ctaid.y;   	// 
	ld.param.s32 	%r4, [__cudaparm___globfunc__Z9matrixMulPfS_S_ii_wA];	// id:85 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_wA+0x0
	mul.lo.s32 	%r5, %r3, %r4;    	// 
	mul.lo.s32 	%r6, %r5, 16;     	// 
	add.s32 	%r7, %r6, %r4;       	// 
	sub.s32 	%r8, %r7, 1;         	// 
	cvt.s32.u16 	%r9, %tid.x;     	// 
	cvt.s32.u16 	%r10, %tid.y;    	// 
	ld.param.s32 	%r11, [__cudaparm___globfunc__Z9matrixMulPfS_S_ii_wB];	// id:83 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_wB+0x0
	setp.lt.s32 	%p1, %r8, %r6;   	// 
	mov.f32 	%f1, 0f00000000;     	// 0
	@%p1 bra 	$Lt_0_13;           	// 
	mov.u32 	%r12, __cuda_Bs20;   	// 
	mov.u32 	%r13, __cuda_As1044; 	// 
	add.s32 	%r14, %r4, 15;       	// 
	shr.s32 	%r15, %r14, 31;      	// 
	mov.s32 	%r16, 15;            	// 
	and.b32 	%r17, %r15, %r16;    	// 
	add.s32 	%r18, %r17, %r14;    	// 
	shr.s32 	%r19, %r18, 4;       	// 
	mul.lo.s32 	%r20, %r10, %r11; 	// 
	mul.lo.s32 	%r21, %r10, %r4;  	// 
	mul24.lo.u32 	%r22, %r10, 16; 	// 
	mul24.lo.u32 	%r23, %r10, 64; 	// 
	mul24.lo.u32 	%r24, %r9, 4;   	// 
	mul.lo.s32 	%r25, %r11, 16;   	// 
	add.s32 	%r26, %r20, %r2;     	// 
	add.s32 	%r27, %r21, %r6;     	// 
	add.u32 	%r28, %r9, %r22;     	// 
	add.u32 	%r29, %r23, %r13;    	// 
	add.u32 	%r30, %r24, %r12;    	// 
	add.s32 	%r31, %r26, %r9;     	// 
	add.s32 	%r32, %r27, %r9;     	// 
	mul.lo.u32 	%r33, %r28, 4;    	// 
	mul.lo.u32 	%r34, %r31, 4;    	// 
	mul.lo.u32 	%r35, %r25, 4;    	// 
	mul.lo.u32 	%r36, %r32, 4;    	// 
	add.u32 	%r37, %r33, %r13;    	// 
	add.u32 	%r38, %r33, %r12;    	// 
	add.s32 	%r39, %r21, %r8;     	// 
	ld.param.u32 	%r40, [__cudaparm___globfunc__Z9matrixMulPfS_S_ii_B];	// id:88 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_B+0x0
	add.u32 	%r41, %r40, %r34;    	// 
	ld.param.u32 	%r42, [__cudaparm___globfunc__Z9matrixMulPfS_S_ii_A];	// id:80 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_A+0x0
	add.u32 	%r43, %r36, %r42;    	// 
	add.s32 	%r44, %r39, %r9;     	// 
	mul.lo.u32 	%r45, %r44, 4;    	// 
	add.u32 	%r46, %r45, %r42;    	// 
	mov.s32 	%r47, %r19;          	// 
$Lt_0_11:
 //<loop> Loop body line 91, nesting depth: 1, estimated iterations: unknown
	.loc	14	106	0
	ld.global.f32 	%f2, [%r43+0]; 	// id:89
	st.shared.f32 	[%r37+0], %f2; 	// id:90 __cuda_As1044+0x0
	.loc	14	107	0
	ld.global.f32 	%f3, [%r41+0]; 	// id:91
	st.shared.f32 	[%r38+0], %f3; 	// id:92 __cuda_Bs20+0x0
	.loc	14	110	0
	bar.sync 	0;                  	// 
	.loc	14	116	0
	ld.shared.f32 	%f4, [%r29+0]; 	// id:93 __cuda_As1044+0x0
	ld.shared.f32 	%f5, [%r30+0]; 	// id:94 __cuda_Bs20+0x0
	mad.f32 	%f1, %f4, %f5, %f1;  	// 
	ld.shared.f32 	%f6, [%r29+4]; 	// id:95 __cuda_As1044+0x0
	ld.shared.f32 	%f7, [%r30+64];	// id:96 __cuda_Bs20+0x0
	mad.f32 	%f1, %f6, %f7, %f1;  	// 
	ld.shared.f32 	%f8, [%r29+8]; 	// id:97 __cuda_As1044+0x0
	ld.shared.f32 	%f9, [%r30+128];	// id:98 __cuda_Bs20+0x0
	mad.f32 	%f1, %f8, %f9, %f1;  	// 
	ld.shared.f32 	%f10, [%r29+12];	// id:99 __cuda_As1044+0x0
	ld.shared.f32 	%f11, [%r30+192];	// id:100 __cuda_Bs20+0x0
	mad.f32 	%f1, %f10, %f11, %f1;	// 
	ld.shared.f32 	%f12, [%r29+16];	// id:101 __cuda_As1044+0x0
	ld.shared.f32 	%f13, [%r30+256];	// id:102 __cuda_Bs20+0x0
	mad.f32 	%f1, %f12, %f13, %f1;	// 
	ld.shared.f32 	%f14, [%r29+20];	// id:103 __cuda_As1044+0x0
	ld.shared.f32 	%f15, [%r30+320];	// id:104 __cuda_Bs20+0x0
	mad.f32 	%f1, %f14, %f15, %f1;	// 
	ld.shared.f32 	%f16, [%r29+24];	// id:105 __cuda_As1044+0x0
	ld.shared.f32 	%f17, [%r30+384];	// id:106 __cuda_Bs20+0x0
	mad.f32 	%f1, %f16, %f17, %f1;	// 
	ld.shared.f32 	%f18, [%r29+28];	// id:107 __cuda_As1044+0x0
	ld.shared.f32 	%f19, [%r30+448];	// id:108 __cuda_Bs20+0x0
	mad.f32 	%f1, %f18, %f19, %f1;	// 
	ld.shared.f32 	%f20, [%r29+32];	// id:109 __cuda_As1044+0x0
	ld.shared.f32 	%f21, [%r30+512];	// id:110 __cuda_Bs20+0x0
	mad.f32 	%f1, %f20, %f21, %f1;	// 
	ld.shared.f32 	%f22, [%r29+36];	// id:111 __cuda_As1044+0x0
	ld.shared.f32 	%f23, [%r30+576];	// id:112 __cuda_Bs20+0x0
	mad.f32 	%f1, %f22, %f23, %f1;	// 
	ld.shared.f32 	%f24, [%r29+40];	// id:113 __cuda_As1044+0x0
	ld.shared.f32 	%f25, [%r30+640];	// id:114 __cuda_Bs20+0x0
	mad.f32 	%f1, %f24, %f25, %f1;	// 
	ld.shared.f32 	%f26, [%r29+44];	// id:115 __cuda_As1044+0x0
	ld.shared.f32 	%f27, [%r30+704];	// id:116 __cuda_Bs20+0x0
	mad.f32 	%f1, %f26, %f27, %f1;	// 
	ld.shared.f32 	%f28, [%r29+48];	// id:117 __cuda_As1044+0x0
	ld.shared.f32 	%f29, [%r30+768];	// id:118 __cuda_Bs20+0x0
	mad.f32 	%f1, %f28, %f29, %f1;	// 
	ld.shared.f32 	%f30, [%r29+52];	// id:119 __cuda_As1044+0x0
	ld.shared.f32 	%f31, [%r30+832];	// id:120 __cuda_Bs20+0x0
	mad.f32 	%f1, %f30, %f31, %f1;	// 
	ld.shared.f32 	%f32, [%r29+56];	// id:121 __cuda_As1044+0x0
	ld.shared.f32 	%f33, [%r30+896];	// id:122 __cuda_Bs20+0x0
	mad.f32 	%f1, %f32, %f33, %f1;	// 
	ld.shared.f32 	%f34, [%r29+60];	// id:123 __cuda_As1044+0x0
	ld.shared.f32 	%f35, [%r30+960];	// id:124 __cuda_Bs20+0x0
	mad.f32 	%f1, %f34, %f35, %f1;	// 
	.loc	14	121	0
	bar.sync 	0;                  	// 
	.loc	14	91	0
	add.u32 	%r41, %r35, %r41;    	// 
	add.u32 	%r43, %r43, 64;      	// 
	setp.le.s32 	%p2, %r43, %r46; 	// 
	@%p2 bra 	$Lt_0_11;           	// 
	bra.uni 	$Lt_0_9;             	// 
$Lt_0_13:
	mul.lo.s32 	%r20, %r10, %r11; 	// 
$Lt_0_9:
	.loc	14	127	0
	ld.param.u32 	%r48, [__cudaparm___globfunc__Z9matrixMulPfS_S_ii_C];	// id:125 __cudaparm___globfunc__Z9matrixMulPfS_S_ii_C+0x0
	mul.lo.s32 	%r49, %r11, %r3;  	// 
	add.s32 	%r50, %r1, %r49;     	// 
	mul.lo.s32 	%r51, %r50, 16;   	// 
	add.s32 	%r52, %r20, %r51;    	// 
	add.s32 	%r53, %r9, %r52;     	// 
	mul.lo.u32 	%r54, %r53, 4;    	// 
	add.u32 	%r55, %r48, %r54;    	// 
	st.global.f32 	[%r55+0], %f1; 	// id:126
	.loc	14	128	0
	exit;                         	// 
$LDWend___globfunc__Z9matrixMulPfS_S_ii:
	} // __globfunc__Z9matrixMulPfS_S_ii

";

	}
}

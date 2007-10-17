using System;
using System.Threading;
using CellDotNet;
using SciMark2Cell;

namespace SciMark2
{
	class Benchmarks
	{
		#region MonteCarlo

		public static void Benchmark_Montecarlo_Combined(int n)
		{
			if(SpeContext.HasSpeHardware)
			{
				Benchmark_Montecarlo_Single_Spu(n);
				Benchmark_Montecarlo_Vector_SPU(n);
				Benchmark_Montecarlo_Unrolled_SPU(n);
			}
				Benchmark_Montecarlo_Single(n);
		}

		public static void Benchmark_Montecarlo_Single(int n)
		{
			Stopwatch watch = new Stopwatch();

			float monoPi = MonteCarloSingle.integrate(n);

			watch.start();

			monoPi = MonteCarloSingle.integrate(n);

			watch.stop();

			Console.WriteLine("Monte Carlo, Single: run time: {2}, n={0} pi={1}", n, monoPi, watch.read());
		}

		private delegate float BenchmarkMonteCarloSPUDelegate(int seed, int iterations); 

		public static void Benchmark_Montecarlo_Single_Spu(int n)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloSingleCell.integrate;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, 113, n);

			watch2.stop();

			Console.WriteLine("Monte Carlo, Single, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		public static void Benchmark_Montecarlo_Vector_SPU(int n)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVector.integrate;
			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, 113, n);

			watch2.stop();

			Console.WriteLine("Monte Carlo, Vector, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		public static void Benchmark_Montecarlo_Unrolled_SPU(int n)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVectorUnroled.integrate;
			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, 113, n);

			watch2.stop();

			Console.WriteLine("Monte Carlo, Unroled, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		#endregion

		#region SOR

		public static void Benchmark_SOR_Combined(int n, int N, int M)
		{
			if(SpeContext.HasSpeHardware)
			{
				Benchmark_SOR_Single_SPU(n, N, M);
			}
			else
			{
				Benchmark_SOR_Single(n, N, M);
			}
		}

//		private delegate void SORDelegate(float a, float[][] b, int c);
		private delegate void SORSPUDelegate(float a, MainStorageArea b, int N, int M, int c);
//		private delegate void SORVectorSPUDelegate(float a, Float32Vector[] b, int N, int M, int c);

		public static void Benchmark_SOR_Single(int n, int M, int N)
		{
			Stopwatch watch = new Stopwatch();

			float[][] Q = RandomMatrix(M, N, new RandomSingle(1234)); // TODO random seed

			watch.start();

			SORSingle.execute(1.25f, Q, n);
			
			watch.stop();

			Console.WriteLine("SOR, Single: run time {1}, n={0} M={2} N={3}", n, watch.read(), M, N);
		}

		public static void Benchmark_SOR_Single_SPU(int n, int M, int N)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(N*M))
			{
				float[] Q = RandomVector(M*N, new RandomSingle(1)); // TODO random seed

				for (int i = mem.ArraySegment.Offset, j = 0; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = Q[j];
				}

				watch1.start();

				SORSPUDelegate fun = SORSingleCell.execute;
				CompileContext cc = new CompileContext(fun.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				watch1.stop();

				watch2.start();

				SpeContext.UnitTestRunProgram(cc, 1.25f, mem.GetArea(), M, N, n);

				watch2.stop();

				Console.WriteLine("SOR, Single, SPU: run time {1} compile time {2}, n={0} M={3} N={4}", n, watch2.read(),
				                  watch1.read(), M, N);
			}
		}

		public static void Benchmark_SOR_Vector_SPU(int n, int M, int N)
		{
			if ((N - 2) % 4 != 0)
				throw new ArgumentException("N must be a integer times 4 plus 2");

			int newN = ((N - 2)/4 + 1);

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(M*newN*4))
			{
				float[] Q = RandomVector(M*newN*4, new RandomSingle(1)); // TODO random seed

				for (int i = mem.ArraySegment.Offset, j = 0; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = Q[j];
				}

				Stopwatch watch1 = new Stopwatch();
				Stopwatch watch2 = new Stopwatch();

				watch1.start();

				SORSPUDelegate fun = SORVector.execute;
				CompileContext cc = new CompileContext(fun.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				watch1.stop();

				watch2.start();

				SpeContext.UnitTestRunProgram(cc, 1.25f, mem.GetArea(), M, newN, n);

				watch2.stop();

				Console.WriteLine("SOR, Single, SPU: run time {1} compile time {2}, n={0} M={3} N={4}", n, watch2.read(),
				                  watch1.read(), M, N);
			}
		}

		#endregion

		#region SparseCompRow

		public static void Benchmark_SparchCompRow_Single(int N, int nz, int num_iterations)
		{
//			RandomSingle R = new RandomSingle(1); // TODO seed
//
//			int nr = nz / N; // average number of nonzeros per row
//			int anz = nr * N; // _actual_ number of nonzeros
//
//			float[] x = RandomVector(N, R);
//			float[] y = new float[N];
//
//
//			float[] val = RandomVector(anz, R);
//			int[] col = new int[anz];
//			int[] row = new int[N + 1];
//
//			row[0] = 0;
//			for (int r = 0; r < N; r++)
//			{
//				// initialize elements for row r
//
//				int rowr = row[r];
//				row[r + 1] = rowr + nr;
//				int step = r / nr;
//				if (step < 1)
//					step = 1;
//				// take at least unit steps
//
//
//				for (int i = 0; i < nr; i++)
//					col[rowr + i] = i * step;
//
//			}
//
//			using (AlignedMemory<float> xmem = SpeContext.AllocateAlignedFloat(N))
//			using (AlignedMemory<float> ymem = SpeContext.AllocateAlignedFloat(N))
//			using (AlignedMemory<float> valmem = SpeContext.AllocateAlignedFloat(anz))
//			using (AlignedMemory<int> colmem = SpeContext.AllocateAlignedInt32(anz))
//			using (AlignedMemory<int> rowmem = SpeContext.AllocateAlignedInt32(N + 1))
//			{
//				float[] Q = RandomVector(M*newN*4, new RandomSingle(1)); // TODO random seed
//
//				for (int i = mem.ArraySegment.Offset, j = 0; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
//				{
//					mem.ArraySegment.Array[i] = Q[j];
//				}
//			}
//
//
//			Stopwatch watch = new Stopwatch();
//
//			watch.start();
//
//			SparseCompRowSingle.matmult(y, val, row, col, x, num_iterations);
//
//			watch.stop();
//
//			Console.WriteLine("SparseCompRow, Single: run time {1}, n={0}", num_iterations, watch.read());
		}

		private delegate void SparchCompRowSPUDelegate(
			MainStorageArea y, int ysize, MainStorageArea val, int valsize, MainStorageArea row, int rowsize, MainStorageArea col,
			int colsize, MainStorageArea x, int xsize, int NUM_ITERATIONS);

		public static void Benchmark_SparchCompRow_Single_SPU(int N, int nz, int num_iterations)
		{
			RandomSingle R = new RandomSingle(1); // TODO seed

			float[] x = RandomVector(N, R);
			float[] y = new float[N];

			int nr = nz / N; // average number of nonzeros per row
			int anz = nr * N; // _actual_ number of nonzeros

			float[] val = RandomVector(anz, R);
			int[] col = new int[anz];
			int[] row = new int[N + 1];

			row[0] = 0;
			for (int r = 0; r < N; r++)
			{
				// initialize elements for row r

				int rowr = row[r];
				row[r + 1] = rowr + nr;
				int step = r / nr;
				if (step < 1)
					step = 1;
				// take at least unit steps

				for (int i = 0; i < nr; i++)
					col[rowr + i] = i * step;

			}



			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			SparchCompRowSPUDelegate del = SparseCompRowSingleCell.matmult;
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			SpeContext.UnitTestRunProgram(cc);

//			SparseCompRowSingle.matmult(y, val, row, col, x, num_iterations);

			watch2.stop();

//			Console.WriteLine("SparseCompRow, Single: run time {1}, n={0}", num_iterations, watch2.read());
		}

		#endregion

		#region parallel

		public static void Benchmark_Parallel()
		{
			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();
			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();
			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();

			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();
			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();
			new Thread(delegate(object obj) { Benchmark_Montecarlo_Vector_SPU(10000000); }).Start();
		}

		#endregion

		private static float[][] RandomMatrix(int M, int N, RandomSingle R)
		{
			float[][] A = new float[M][];
			for (int i = 0; i < M; i++)
			{
				A[i] = new float[N];
			}

			for (int i = 0; i < M; i++)
				for (int j = 0; j < N; j++)
					A[i][j] = R.nextFloat();

			return A;
		}

		private static float[] RandomVector(int N, RandomSingle R)
		{
			float[] A = new float[N];

			for (int i = 0; i < N; i++)
				A[i] = R.nextFloat();

			return A;
		}

		private static Float32Vector[] RandomVectorVector(int N, RandomVector R)
		{
			Float32Vector[] A = new Float32Vector[N];

			for (int i = 0; i < N; i++)
				A[i] = R.nextFloat();

			return A;
		}
	}
}

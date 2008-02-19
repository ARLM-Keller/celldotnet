using System;
using System.Text;
using System.Threading;
using CellDotNet;
using SciMark2Cell;

namespace SciMark2
{
	class Benchmarks
	{
		public static void Bencmark_Combined()
		{
			if (SpeContext.HasSpeHardware)
			{
				Benchmark_Startuptime_SPU();
				Benchmark_Startuptime_SPU();
			}
//			Benchmark_Montecarlo_Combined(10000000);
//			Benchmark_Montecarlo_Single_Unrolled(10000000);
//			Benchmark_SOR_Combined(10000, 100, 25 * 4 + 2);
//			Benchmark_SparchCompRow_Combined(1000, 5000, 10000);
//			Benchmark_Parallel_Combined(100000000);
			Benchmark_Parallel_Combined(10000000);
//			Benchmark_Parallel_Combined(1000000000);
		}

		#region MonteCarlo

		public static void Benchmark_Montecarlo_Combined(int n)
		{
			if(SpeContext.HasSpeHardware)
			{
				Benchmark_Montecarlo_Single_Spu(n);
				Benchmark_Montecarlo_Vector_SPU(n);
//				Benchmark_Montecarlo_Dynamic_Unrolled_SPU(n);
				Benchmark_Montecarlo_Unrolled_SPU(n);
//				Benchmark_Montecarlo_Vector_Simple_Unrolled_SPU(n);
			}
				Benchmark_Montecarlo_Single(n);
		}

		public static void Benchmark_Montecarlo_Single(int n)
		{
			Stopwatch watch = new Stopwatch();

			float monoPi1 = MonteCarloSingle.integrate(n);

			watch.start();

			float monoPi2 = MonteCarloSingle.integrate(n);

			watch.stop();

			Console.WriteLine("Monte Carlo, Single: run time: {2}, n={0} pi={1}", n, monoPi2, watch.read(), monoPi1);
		}

		public static void Benchmark_Montecarlo_Single_Unrolled(int n)
		{
			Stopwatch watch = new Stopwatch();

			float monoPi1 = MonteCarloSingleUnrolled.integrate(n);

			watch.start();

			float monoPi2 = MonteCarloSingleUnrolled.integrate(n);

			watch.stop();

			Console.WriteLine("Monte Carlo, Single Unrolled: run time: {2}, n={0} pi={1}", n, monoPi2, watch.read(), monoPi1);
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

			cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_Montecarlo_Single_Spu_113_{0}.s", 10000).ToString(), 113, 10000);

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

			cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_Montecarlo_Vector_Spu_113_{0}.s", 10000).ToString(), 113, 10000);

			Console.WriteLine("Monte Carlo, Vector, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		public static void Benchmark_Montecarlo_Dynamic_Unrolled_SPU(int n)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVectorDynamicUnroled.integrate;
			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, 113, n);

			watch2.stop();

			cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_Montecarlo_Dynamic_Unrolled_Spu_113_{0}.s", 10000).ToString(), 113, 10000);

			Console.WriteLine("Monte Carlo, dynamic Unroled, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
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

			cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_Montecarlo_Unrolled_Spu_113_{0}.s", 10000).ToString(), 113, 10000);

			Console.WriteLine("Monte Carlo, Unroled, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		public static void Benchmark_Montecarlo_Vector_Simple_Unrolled_SPU(int n)
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVectorSimpleUnroled.integrate;
			CompileContext cc = new CompileContext(fun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, 113, n);

			watch2.stop();

			cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_Montecarlo_Simple_Unrolled_Spu_113_{0}.s", 10000).ToString(), 113, 10000);

			Console.WriteLine("Monte Carlo, Simple Unroled, SPU: run time: {3}, compile time: {2}, n={0} pi={1} ", n, spuPi, watch1.read(), watch2.read());
		}

		#endregion

		#region SOR

		public static void Benchmark_SOR_Combined(int n, int N, int M)
		{
			if(SpeContext.HasSpeHardware)
			{
				Benchmark_SOR_Single_SPU(n, N, M);
				Benchmark_SOR_Vector_SPU(n, N, M);
			}
				Benchmark_SOR_Single(n, N, M);
		}

//		private delegate void SORDelegate(float a, float[][] b, int c);
		private delegate void SORSPUDelegate(float a, MainStorageArea b, int N, int M, int c);
//		private delegate void SORVectorSPUDelegate(float a, Float32Vector[] b, int N, int M, int c);

		public static void Benchmark_SOR_Single(int n, int M, int N)
		{
			Stopwatch watch = new Stopwatch();

			float[][] Q = RandomMatrix(M, N, new RandomSingle(1234)); // TODO random seed
			SORSingle.execute(1.25f, Q, n);

			Q = RandomMatrix(M, N, new RandomSingle(1234)); // TODO random seed

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

				cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_SOR_Single_SPU_1.25_{0}_{1}_{2}.s", M, N, n).ToString(), 1.25f, 0, M, N, n);

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

				cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_SOR_Vector_SPU_1.25_{0}_{1}_{2}.s", M, N, n).ToString(), 1.25f, 0, M, N, n);

				Console.WriteLine("SOR, Vector, SPU: run time {1} compile time {2}, n={0} M={3} N={4}", n, watch2.read(),
				                  watch1.read(), M, N);
			}
		}

		#endregion

		#region SparseCompRow

		public static void Benchmark_SparchCompRow_Combined(int N, int nz, int num_iterations)
		{
			if(SpeContext.HasSpeHardware)
			{
				Benchmark_SparchCompRow_Single_SPU(N, nz, num_iterations);
			}

			Benchmark_SparchCompRow_Single(N, nz, num_iterations);
		}

		private delegate void SparchCompRowSPUDelegate(
			MainStorageArea y, int ysize, MainStorageArea val, int valsize, MainStorageArea row, int rowsize, MainStorageArea col,
			int colsize, MainStorageArea x, int xsize, int NUM_ITERATIONS);

		public static void Benchmark_SparchCompRow_Single(int N, int nz, int num_iterations)
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

			Stopwatch watch = new Stopwatch();

			SparseCompRowSingle.matmult(y, val, row, col, x, num_iterations);

			watch.start();

			SparseCompRowSingle.matmult(y, val, row, col, x, num_iterations);

			watch.stop();

			Console.WriteLine("SparseCompRow, Single: run time {0}, n={1}", watch.read(), num_iterations);
		}

		public static void Benchmark_SparchCompRow_Single_SPU(int N, int nz, int num_iterations)
		{
			RandomSingle R = new RandomSingle(1); // TODO seed

			int nr = nz / N; // average number of nonzeros per row
			int anz = nr * N; // _actual_ number of nonzeros

			float[] x = RandomVector(N, R);
			float[] y = new float[N];


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

			using (AlignedMemory<float> xmem = SpeContext.AllocateAlignedFloat(N))
			using (AlignedMemory<float> ymem = SpeContext.AllocateAlignedFloat(N))
			using (AlignedMemory<float> valmem = SpeContext.AllocateAlignedFloat(anz))
			using (AlignedMemory<int> colmem = SpeContext.AllocateAlignedInt32(anz))
			using (AlignedMemory<int> rowmem = SpeContext.AllocateAlignedInt32(N + 1))
			{
				// TODO faster copy or genarate data directly in thoes arrays.
				for (int i = xmem.ArraySegment.Offset, j = 0; i < xmem.ArraySegment.Offset + xmem.ArraySegment.Count; i++, j++)
					xmem.ArraySegment.Array[i] = x[j];
				for (int i = ymem.ArraySegment.Offset, j = 0; i < ymem.ArraySegment.Offset + ymem.ArraySegment.Count; i++, j++)
					ymem.ArraySegment.Array[i] = y[j];
				for (int i = valmem.ArraySegment.Offset, j = 0; i < valmem.ArraySegment.Offset + valmem.ArraySegment.Count; i++, j++)
					valmem.ArraySegment.Array[i] = val[j];
				for (int i = colmem.ArraySegment.Offset, j = 0; i < colmem.ArraySegment.Offset + colmem.ArraySegment.Count; i++, j++)
					colmem.ArraySegment.Array[i] = col[j];
				for (int i = rowmem.ArraySegment.Offset, j = 0; i < rowmem.ArraySegment.Offset + rowmem.ArraySegment.Count; i++, j++)
					rowmem.ArraySegment.Array[i] = row[j];

				Stopwatch watch1 = new Stopwatch();
				Stopwatch watch2 = new Stopwatch();

				watch1.start();

				SparchCompRowSPUDelegate fun = SparseCompRowSingleCell.matmult;
				CompileContext cc = new CompileContext(fun.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				watch1.stop();

				watch2.start();

				SpeContext.UnitTestRunProgram(cc, ymem.GetArea(), y.Length, valmem.GetArea(), val.Length, rowmem.GetArea(),
				                              row.Length, colmem.GetArea(), col.Length, xmem.GetArea(), x.Length, num_iterations);

				watch2.stop();

				cc.WriteAssemblyToFile(new StringBuilder().AppendFormat("Benchmark_SCR_Single_SPU---.s").ToString(), ymem.GetArea(), y.Length, valmem.GetArea(), val.Length, rowmem.GetArea(),
											  row.Length, colmem.GetArea(), col.Length, xmem.GetArea(), x.Length, num_iterations);

				Console.WriteLine("SparseCompRow, Single, SPU: run time {0} compile time {1}, n={2}", watch2.read(), watch1.read(), num_iterations);
			}
		}

		#endregion

		#region parallel

		public static void Benchmark_Parallel_Combined(int n)
		{
			if (SpeContext.HasSpeHardware)
			{
				Benchmark_Montecarlo_Parallel_Cell(n);
			}
			else
			{
				Benchmark_Montecarlo_Parallel2(n);
//				Benchmark_Montecarlo_Parallel4(n);
			}
		}

		public static void Benchmark_Montecarlo_Parallel_Cell(int n)
		{

			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVector.integrate;

			CompileContext cc1 = new CompileContext(fun.Method);
			CompileContext cc2 = new CompileContext(fun.Method);
			CompileContext cc3 = new CompileContext(fun.Method);
			CompileContext cc4 = new CompileContext(fun.Method);
			CompileContext cc5 = new CompileContext(fun.Method);
			CompileContext cc6 = new CompileContext(fun.Method);

			cc1.PerformProcessing(CompileContextState.S8Complete);
			cc2.PerformProcessing(CompileContextState.S8Complete);
			cc3.PerformProcessing(CompileContextState.S8Complete);
			cc4.PerformProcessing(CompileContextState.S8Complete);
			cc5.PerformProcessing(CompileContextState.S8Complete);
			cc6.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			float spuPi1=0, spuPi2=0, spuPi3=0, spuPi4=0, spuPi5=0, spuPi6=0;

			Thread t1 = new Thread(delegate(object obj) { spuPi1 = (float) SpeContext.UnitTestRunProgram(cc1, 110, n / 6); });
			Thread t2 = new Thread(delegate(object obj) { spuPi2 = (float) SpeContext.UnitTestRunProgram(cc2, 111, n / 6); });
			Thread t3 = new Thread(delegate(object obj) { spuPi3 = (float) SpeContext.UnitTestRunProgram(cc3, 112, n / 6); });
			Thread t4 = new Thread(delegate(object obj) { spuPi4 = (float) SpeContext.UnitTestRunProgram(cc4, 113, n / 6); });
			Thread t5 = new Thread(delegate(object obj) { spuPi5 = (float) SpeContext.UnitTestRunProgram(cc5, 114, n / 6); });
			Thread t6 = new Thread(delegate(object obj) { spuPi6 = (float) SpeContext.UnitTestRunProgram(cc6, 115, n / 6); });

			t1.Start();
			t2.Start();
			t3.Start();
			t4.Start();
			t5.Start();
			t6.Start();

			t1.Join();
			t2.Join();
			t3.Join();
			t4.Join();
			t5.Join();
			t6.Join();

			watch2.stop();

			Console.WriteLine("Monte Carlo, Unrolled, Cell, Parallel: run time: {2}, compile time: {1}, n={0} pi={3} ", n, watch1.read(), watch2.read(), (spuPi1 + spuPi2 + spuPi3 + spuPi4 + spuPi5 + spuPi6)/6);
		}

		public static void Benchmark_Montecarlo_Parallel4(int n)
		{

			Stopwatch watch1 = new Stopwatch();

			watch1.start();

			float spuPi1 = 0, spuPi2 = 0, spuPi3 = 0, spuPi4 = 0;

			Thread t1 = new Thread(delegate(object obj) { spuPi1 = MonteCarloSingle.integrate(n / 4); });
			Thread t2 = new Thread(delegate(object obj) { spuPi2 = MonteCarloSingle.integrate(n / 4); });
			Thread t3 = new Thread(delegate(object obj) { spuPi3 = MonteCarloSingle.integrate(n / 4); });
			Thread t4 = new Thread(delegate(object obj) { spuPi4 = MonteCarloSingle.integrate(n / 4); });

			t1.Start();
			t2.Start();
			t3.Start();
			t4.Start();

			t1.Join();
			t2.Join();
			t3.Join();
			t4.Join();

			watch1.stop();

			Console.WriteLine("Monte Carlo, Parallel4: run time: {1}, n={0} pi={2} ", n, watch1.read(), (spuPi1 + spuPi2 + spuPi3 + spuPi4) / 4);
		}

		public static void Benchmark_Montecarlo_Parallel2(int n)
		{

			Stopwatch watch1 = new Stopwatch();

			watch1.start();

			float spuPi1=0, spuPi2=0;

			Thread t1 = new Thread(delegate(object obj) { spuPi1 = MonteCarloSingle.integrate(n / 2); });
			Thread t2 = new Thread(delegate(object obj) { spuPi2 = MonteCarloSingle.integrate(n / 2); });

			t1.Start();
			t2.Start();

			t1.Join();
			t2.Join();

			watch1.stop();

			Console.WriteLine("Monte Carlo, Parallel2: run time: {1}, n={0} pi={2} ", n, watch1.read(), (spuPi1 + spuPi2) / 2);
		}

		#endregion


		#region misc

		private static int SimpleMethod(int n)
		{
			return n;
		}

		public static void Benchmark_Startuptime_SPU()
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();

			watch1.start();

			Converter<int, int> fun = SimpleMethod;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			int n = (int)SpeContext.UnitTestRunProgram(cc, 4213);

			watch2.stop();

			Console.WriteLine("Startuptime, SPU: run time: {1}, compile time: {0}", watch1.read(), watch2.read());
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

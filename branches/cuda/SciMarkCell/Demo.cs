using System;
using CellDotNet;
using RandomSingle=SciMark2.RandomSingle;

namespace SciMark2Cell 
{
	class Demo
	{
		static public void Main(string[] args)
		{
			Benchmark_Montecarlo_Single_Spu(10000000);
			Benchmark_Montecarlo_Vector_Spu(10000000);
			Benchmark_SOR_Single_Spu(100, 102, 10000);
			Benchmark_SOR_Vector_Spu(100, 102, 10000);
		}

		private delegate float BenchmarkMonteCarloSPUDelegate(int seed, int n);

		public static void Benchmark_Montecarlo_Single_Spu(int n)
		{
			BenchmarkMonteCarloSPUDelegate fun = MonteCarloSingleCell.integrate;

			float spuPi = (float)SpeContext.UnitTestRunProgram(fun, 113, n);

			Console.WriteLine("Monte Carlo, Single, SPU: n={0} pi={1} ", n, spuPi);
		}

		public static void Benchmark_Montecarlo_Vector_Spu(int n)
		{
			BenchmarkMonteCarloSPUDelegate fun = MonteCarloVector.integrate;

			float spuPi = (float)SpeContext.UnitTestRunProgram(fun, 113, n);

			Console.WriteLine("Monte Carlo, Vector, SPU: n={0} pi={1} ", n, spuPi);
		}

		private delegate void SORSPUDelegate(float omega, MainStorageArea G, int M, int N, int n);

		public static void Benchmark_SOR_Single_Spu(int M, int N, int n)
		{
			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(N * M))
			{
				RandomSingle rand = new RandomSingle(1); // TODO random seed

				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = rand.nextFloat();

				SORSPUDelegate fun = SORSingleCell.execute;

				SpeContext.UnitTestRunProgram(fun, 1.25f, mem.GetArea(), M, N, n);

				Console.WriteLine("SOR, Single, SPU:n={0} M={1} N={2}", n, M, N);
			}
		}

		public static void Benchmark_SOR_Vector_Spu(int M, int N, int n)
		{
			int newN = (N - 2)/4 + 1;

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(M * newN * 4))
			{
				RandomSingle rand = new RandomSingle(1); // TODO random seed

				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = rand.nextFloat();

				SORSPUDelegate fun = SORSingleCell.execute;

				SpeContext.UnitTestRunProgram(fun, 1.25f, mem.GetArea(), M, newN, n);

				Console.WriteLine("SOR, Single, SPU:n={0} M={1} N={2}", n, M, N);
			}
		}
	}
}

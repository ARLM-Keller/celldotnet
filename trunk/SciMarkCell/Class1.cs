using System;
using CellDotNet;
using SciMark2;

namespace SciMark2Cell
{
	class Class1
	{
		static public void Main(string[] args)
		{
//			BenchMarkSingleSpu();
//
//			BenchMarkSingle();
//
//			SpuVectorBenchMark();
//
//			SpuVectorUnroledBenchMark();

//			Benchmarks.Benchmark_SOR_Single(10000, 100, 25 * 4 + 2);
//			Benchmarks.Benchmark_SOR_Single_SPU(10000, 100, 25 * 4 + 2);
//			Benchmarks.Benchmark_SOR_Vector_SPU(10000, 100, 25 * 4 + 2);

//			Benchmarks.Benchmark_Parallel();

//			Benchmarks.Benchmark_SparchCompRow_Single(1000, 5000, 10000);

//			Benchmarks.Benchmark_Montecarlo_Combined(10000000);

//			Benchmarks.Benchmark_Montecarlo_Vector_SPU(1000000);

//			Benchmarks.Benchmark_SparchCompRow_Combined();

			Benchmarks.Bencmark_Combined();

//			Benchmarks.Benchmark_Montecarlo_Unrolled_SPU(1234);

			Console.WriteLine("");
		}


//		public static void BenchMarkSingleSpu()
//		{
//			Stopwatch watch1 = new Stopwatch();
//			Stopwatch watch2 = new Stopwatch();
//			Stopwatch watch3 = new Stopwatch();
//			int n = 10000000;
//
//			watch1.start();
//
//			Converter<int, float> fun = MonteCarloSingle.integrate;
//
//			CompileContext cc = new CompileContext(fun.Method);
//
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
//			watch1.stop();
//
//			watch2.start();
//
//			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);
//
//			watch2.stop();
//
//			Console.WriteLine("SPU: MonetCarlo result n={0} pi={1}", n, spuPi);
//			Console.WriteLine("SPU: Compile time: {0} run time {1}", watch1.read(), watch2.read());
//
//		}
//
//		public static void BenchMarkSingle()
//		{
//			Stopwatch watch1 = new Stopwatch();
//			Stopwatch watch2 = new Stopwatch();
//			Stopwatch watch3 = new Stopwatch();
//			int n = 10000000;
//
//			float monoPi = MonteCarloSingle.integrate(n);
//
//			watch3.start();
//
//			monoPi = MonteCarloSingle.integrate(n);
//
//			watch3.stop();
//
//			Console.WriteLine("Mono: MonetCarlo result n={0} pi={1}", n, monoPi);
//			Console.WriteLine("Mono: run time {0}", watch3.read());
//		}

//		public static void SpuVectorBenchMark()
//		{
//			Stopwatch watch1 = new Stopwatch();
//			Stopwatch watch2 = new Stopwatch();
//
////			int n = 10000000;
//			int n = 10000;
//
//			watch1.start();
//
//			Converter<int, float> fun = MonteCarloVector.integrate;
//
//			CompileContext cc = new CompileContext(fun.Method);
//
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
////			cc.DisassembleToConsole();
//
//			cc.WriteAssemblyToFile("SpuVectorBenchMark.s", n);
//
//			watch1.stop();
//
//			watch2.start();
//
//			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);
//
//			watch2.stop();
//
//			Console.WriteLine("SpuVectorBenchMark: MonetCarlo result n={0} pi={1}", n, spuPi);
//			Console.WriteLine("SpuVectorBenchMark: Compile time: {0} run time {1}", watch1.read(), watch2.read());
//		}

//		public static void SpuVectorUnroledBenchMark()
//		{
//			Stopwatch watch1 = new Stopwatch();
//			Stopwatch watch2 = new Stopwatch();
//
//			int n = 10000;
////			int n = 10000000;
//
//			watch1.start();
//
//			Converter<int, float> fun = MonteCarloVectorUnroled.integrate;
//
//			CompileContext cc = new CompileContext(fun.Method);
//
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
////			cc.DisassembleToConsole();
//
//			cc.WriteAssemblyToFile("SpuVectorUnroledBenchMark.s", n);
//
//			watch1.stop();
//
//			watch2.start();
//
//			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);
//
//			watch2.stop();
//
//			Console.WriteLine("SpuVectorUnroledBenchMark: MonetCarlo result n={0} pi={1}", n, spuPi);
//			Console.WriteLine("SpuVectorUnroledBenchMark: Compile time: {0} run time {1}", watch1.read(), watch2.read());
//		}


	}
}

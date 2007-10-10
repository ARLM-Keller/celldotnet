using System;
using CellDotNet;
using SciMark2;

namespace SciMark2Cell
{
	class Class1
	{
		static public void Main(string[] args)
		{
//			new MonteCarloTest().TestMonteCarloSingle();
//			new MonteCarloDoubleTest().TestMonteCarloSingle();

//			new FFTTest().TestFFTSmall();

			BenchMark();

			SpuVectorBenchMark();

			Console.WriteLine("");
		}


		public static void BenchMark()
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();
			Stopwatch watch3 = new Stopwatch();

			watch1.start();

			Converter<int, float> fun = MonteCarloSingleCell.integrate;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			int n = 10000000;

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);

			watch2.stop();

			Console.WriteLine("SPU: MonetCarlo result n={0} pi={1}", n, spuPi);
			Console.WriteLine("SPU: Compile time: {0} run time {1}", watch1.read(), watch2.read());

			float monoPi = MonteCarloSingleCell.integrate(n);

			watch3.start();

			monoPi  = MonteCarloSingleCell.integrate(n);

			watch3.stop();

			Console.WriteLine("Mono: MonetCarlo result n={0} pi={1}", n, monoPi);
			Console.WriteLine("Mono: run time {0}", watch3.read());
		}

		public static void SpuVectorBenchMark()
		{
			Stopwatch watch1 = new Stopwatch();
			Stopwatch watch2 = new Stopwatch();
			
			watch1.start();

			Converter<int, float> fun = MonteCarloVector.integrate;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			int n = 10000000;

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);

			watch2.stop();

			Console.WriteLine("SPU: MonetCarlo result n={0} pi={1}", n, spuPi);
			Console.WriteLine("SPU: Compile time: {0} run time {1}", watch1.read(), watch2.read());
		}



	}
}

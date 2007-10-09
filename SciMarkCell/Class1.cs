using System;
using CellDotNet;

namespace SciMarkCell
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

			System.Console.WriteLine("");
		}


		public static void BenchMark()
		{
			SciMarkCell.Stopwatch watch1 = new Stopwatch();
			SciMarkCell.Stopwatch watch2 = new Stopwatch();
			SciMarkCell.Stopwatch watch3 = new Stopwatch();

			watch1.start();

			Converter<int, float> fun = MonteCarlo.integrate;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			watch1.stop();

			watch2.start();

			int n = 10000000;

			float spuPi = (float)SpeContext.UnitTestRunProgram(cc, n);

			watch2.stop();

			Console.WriteLine("SPU: MonetCarlo result n={0} pi={1}", n, spuPi);
			Console.WriteLine("SPU: Compile time: {0} run time {1}", watch1.read(), watch2.read());

			float monoPi = MonteCarlo.integrate(n);

			watch3.start();

			monoPi  = MonteCarlo.integrate(n);

			watch3.stop();

			Console.WriteLine("Mono: MonetCarlo result n={0} pi={1}", n, monoPi);
			Console.WriteLine("Mono: run time {0}", watch3.read());
		}

		public static void SpuVectorBenchMark()
		{
			SciMarkCell.Stopwatch watch1 = new Stopwatch();
			SciMarkCell.Stopwatch watch2 = new Stopwatch();
			
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

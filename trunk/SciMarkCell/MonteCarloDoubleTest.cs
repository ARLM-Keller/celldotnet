using System;
using NUnit.Framework;
using SciMarkCell;
using CellDotNet;

namespace SciMarkCell
{
	[TestFixture]
	public class MonteCarloDoubleTest
	{
		[Test]
		public void TestMonteCarloSingle()
		{
			Converter<int, double> fun = MonteCarloDouble.integrate;

			CompileContext cc = new CompileContext(fun.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			int n = 1000;

			object result = SpeContext.UnitTestRunProgram(cc, n);

			Console.WriteLine("MonetCarlo result n={0} pi={1}", n, (float)result);
		}
	}
}
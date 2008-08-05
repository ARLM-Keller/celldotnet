using System;
using System.Linq;
using NUnit.Framework;

namespace CellDotNet.Spe
{
	[TestFixture]
	public class SystemMathTest : UnitTest
	{
		[Test]
		public void TestSin()
		{
			Func<double, double> del = d => Math.Sin(d);

			double arg = 25;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);

		}
		[Test]
		public void TestCos()
		{
			Func<double, double> del = d => Math.Cos(d);

			double arg = 25;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestTan()
		{
			Func<double, double> del = d => Math.Tan(d);

			double arg = 25;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestAsin()
		{
			Func<double, double> del = d => Math.Asin(d);

			double arg = -.4;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestAcos()
		{
			Func<double, double> del = d => Math.Acos(d);

			double arg = -.5;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestAtan()
		{
			Func<double, double> del = d => Math.Atan(d);

			double arg = -.5;
			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestAtan2()
		{
			Func<double, double, double> del = (x, y) => Math.Atan2(x, y);

			double arg1 = -.5;
			double arg2 = 3;

			AreWithinLimits(del(arg1, arg2), (double)SpeContext.UnitTestRunProgram(del, arg1, arg2), 0.000001, null);
		}

		[Test]
		public void TestSqrt()
		{
			Func<double, double> del = x => Math.Sqrt(x);

			double arg = 3;

			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}

		[Test]
		public void TestLog()
		{
			Func<double, double> del = x => Math.Log(x);

			double arg = 15;

			AreWithinLimits(del(arg), (double)SpeContext.UnitTestRunProgram(del, arg), 0.000001, null);
		}
	}
}

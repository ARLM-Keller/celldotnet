// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if UNITTEST

using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;



namespace CellDotNet
{
	/// <summary>
	/// Base class for unit tests with convenient methods.
	/// </summary>
	public abstract class UnitTest
	{
		protected static void AreEqual<T>(T expected, T actual, string message)
		{
			Assert.AreEqual(expected, actual, message);
		}

		protected static void AreEqual<T>(T expected, T actual)
		{
			Assert.AreEqual(expected, actual);
		}

		protected static void AreNotEqual<T>(T expected, T actual, string message)
		{
			Assert.AreNotEqual(expected, actual, message);
		}

		protected static void AreNotEqual<T>(T expected, T actual)
		{
			Assert.AreNotEqual(expected, actual);
		}

		protected static void AreSame<T1, T2>(T1 expected, T2 actual) 
			where T1 : class where T2 : T1
		{
			Assert.AreSame(expected, actual);
		}

		protected static void IsTrue(bool condition, string message)
		{
			Assert.IsTrue(condition, message);
		}

		protected static void IsTrue(bool condition)
		{
			Assert.IsTrue(condition);
		}

		protected static void IsFalse(bool condition, string message)
		{
			Assert.IsFalse(condition, message);
		}

		protected static void IsFalse(bool condition)
		{
			Assert.IsFalse(condition);
		}

		protected static void IsNull(object value)
		{
			Assert.IsNull(value);
		}

		protected static void IsNull(object value, string message)
		{
			Assert.IsNull(value, message);
		}

		protected static void IsNotNull(object value)
		{
			Assert.IsNotNull(value);
		}

		protected static void IsNotNull(object value, string message)
		{
			Assert.IsNotNull(value, message);
		}

		protected static void Fail()
		{
			Assert.Fail();
		}

		protected static void Fail(string message)
		{
			Assert.Fail(message);
		}

		protected bool HasUnixShell
		{
			get { return SpeContext.HasSpeHardware; }
		}

		static public string GetUnitTestName()
		{
			StackTrace st = new StackTrace(0);
			StackFrame[] frames = st.GetFrames();

			foreach (StackFrame f in frames)
			{
				MethodBase m = f.GetMethod();
				if (m.IsDefined(typeof (TestAttribute), false))
				{
					return m.Name;
				}
			}
			throw new InvalidOperationException("Not in nunit test.");
		}

		/// <summary>
		/// If <paramref name="target" /> &lt; <paramref name="error"/> then it is sufficient for <paramref name="value"/> to be within +/- <paramref name="error"/> from 0.
		/// </summary>
		static public void AreWithinLimits(float target, float value, float error, string message)
		{
			if (!(Math.Abs(target) < error && Math.Abs(value) < error))
				if (Math.Abs(value) > Math.Abs(target) * (1 + error) || Math.Abs(value) < Math.Abs(target) * (1 - error) || Math.Sign(value) != Math.Sign(target))
					throw new DebugAssertException(message);
		}

		/// <summary>
		/// If <paramref name="target" /> &lt; <paramref name="error"/> then it is sufficient for <paramref name="value"/> to be within +/- <paramref name="error"/> from 0.
		/// </summary>
		static public void AreWithinLimits(double target, double value, double error, string message)
		{
			if (!(Math.Abs(target) < error && Math.Abs(value) < error))
				if (Math.Abs(value) > Math.Abs(target) * (1 + error) || Math.Abs(value) < Math.Abs(target) * (1 - error) || Math.Sign(value) != Math.Sign(target))
					throw new DebugAssertException(message);
		}

		static public void AreWithinLimits(Float32Vector target, Float32Vector value, float error, string message)
		{
			if (!(Math.Abs(target.E1) < error && Math.Abs(value.E1) < error))
				if (Math.Abs(value.E1) > Math.Abs(target.E1) * (1 + error) || Math.Abs(value.E1) < Math.Abs(target.E1) * (1 - error) || Math.Sign(value.E1) != Math.Sign(target.E1))
					throw new DebugAssertException(message);

			if (!(Math.Abs(target.E2) < error && Math.Abs(value.E2) < error))
				if (Math.Abs(value.E2) > Math.Abs(target.E2) * (1 + error) || Math.Abs(value.E2) < Math.Abs(target.E2) * (1 - error) || Math.Sign(value.E2) != Math.Sign(target.E2))
					throw new DebugAssertException(message);

			if (!(Math.Abs(target.E3) < error && Math.Abs(value.E3) < error))
				if (Math.Abs(value.E3) > Math.Abs(target.E3) * (1 + error) || Math.Abs(value.E3) < Math.Abs(target.E3) * (1 - error) || Math.Sign(value.E3) != Math.Sign(target.E3))
					throw new DebugAssertException(message);

			if (!(Math.Abs(target.E4) < error && Math.Abs(value.E4) < error))
				if (Math.Abs(value.E4) > Math.Abs(target.E4) * (1 + error) || Math.Abs(value.E4) < Math.Abs(target.E4) * (1 - error) || Math.Sign(value.E4) != Math.Sign(target.E4))
					throw new DebugAssertException(message);
		}

		static public void AreWithinLimits(Float64Vector target, Float64Vector value, double error, string message)
		{
			if (!(Math.Abs(target.E1) < error && Math.Abs(value.E1) < error))
				if (Math.Abs(value.E1) > Math.Abs(target.E1) * (1 + error) || Math.Abs(value.E1) < Math.Abs(target.E1) * (1 - error) || Math.Sign(value.E1) != Math.Sign(target.E1))
					throw new DebugAssertException(message);

			if (!(Math.Abs(target.E2) < error && Math.Abs(value.E2) < error))
				if (Math.Abs(value.E2) > Math.Abs(target.E2) * (1 + error) || Math.Abs(value.E2) < Math.Abs(target.E2) * (1 - error) || Math.Sign(value.E2) != Math.Sign(target.E2))
					throw new DebugAssertException(message);
		}
	}
}
#endif
using System;
using System.Collections.Generic;
using System.Text;
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
			where T1 : class where T2 : class
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
	}
}

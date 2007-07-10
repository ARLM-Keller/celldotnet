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
		protected static void AreEqual<T>(T x, T y, string message)
		{
			Assert.AreEqual(x, y, message);
		}

		protected static void AreEqual<T>(T x, T y)
		{
			Assert.AreEqual(x, y);
		}

		protected static void AreNotEqual<T>(T x, T y, string message)
		{
			Assert.AreNotEqual(x, y, message);
		}

		protected static void AreNotEqual<T>(T x, T y)
		{
			Assert.AreNotEqual(x, y);
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

		protected static void Fail()
		{
			Assert.Fail();
		}

		protected static void Fail(string message)
		{
			Assert.Fail(message);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CellDotNet
{
	static class Utilities
	{
		#region Asserts.

		static public void AssertArgumentNotNull(object arg, string paramName)
		{
			if (arg == null)
				throw new ArgumentNullException(paramName);
		}

		static public void AssertArgument(bool condition, string message)
		{
			if (!condition)
				throw new ArgumentException(message);
		}

		static public void AssertNotNull(object arg, string expressionOrMessage)
		{
			if (arg == null)
				throw new Exception("An expression is null: " + expressionOrMessage);
		}

		static public void Assert(bool condition, string message)
		{
			if (!condition)
				throw new Exception(message); // Should we use another exception?
		}

		#endregion

		static public bool TryGetFirst<T>(IEnumerable<T> enumerable, out T first)
		{
			first = default(T);

			using (IEnumerator<T> e = enumerable.GetEnumerator())
			{
				if (!e.MoveNext())
					return false;

				first = e.Current;
				return true;
			}
		}

		public static T GetFirst<T>(IEnumerable<T> set)
		{
			T first;

			if (!TryGetFirst(set, out first))
				throw new ArgumentException("Empty set.");

			return first;
		}

//		public static IEnumerable<TTarget> CastEnumerable<TSource, TTarget>(IEnumerable<TSource> source) where TSource : TTarget
//		{
//			foreach (TSource tSource in source)
//			{
//				yield return tSource;
//			}
//		}

		public static int Align16(int value)
		{
			return (value + 15) & ~0xf;
		}

		/// <summary>
		/// You can use this one to make resharper think that a variable is used so that it won't
		/// show a warning. Can be handy when the variable isn't used for anything but debugging.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="var"></param>
		[Conditional("DEBUG")]
		public static void PretendVariableIsUsed<T>(T var)
		{
			
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	public static class Utilities
	{
		#region Asserts.

		static public void AssertArgumentNotNull(object arg, string paramName)
		{
			if (arg == null)
				throw new ArgumentNullException(paramName);
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

		static public void AssertArgument(bool condition, string message)
		{
			if (!condition)
				throw new ArgumentException(message);
		}

		static public void AssertArgumentRange(bool rangeCondition, string paramName, object actualValue, string message)
		{
			if (!rangeCondition)
				throw new ArgumentOutOfRangeException(paramName, actualValue, message);
		}

		static public void AssertArgumentRange(bool rangeCondition, string paramName, object actualValue)
		{
			if (!rangeCondition)
				throw new ArgumentOutOfRangeException(paramName, actualValue, "The value is out of range.");
		}

		static public void AssertNotNull(object arg, string expressionOrMessage)
		{
			if (arg == null)
				throw new DebugAssertException("An expression is null: " + expressionOrMessage);
		}

		static public void AssertNull(object arg, string expressionOrMessage)
		{
			if (arg != null)
				throw new DebugAssertException("An expression is not null: " + expressionOrMessage);
		}

		static public void Assert(bool condition, string message)
		{
			if (!condition)
				throw new DebugAssertException(message);
		}

		/// <summary>
		/// Note, if <code>target &lt; error</code> then it is sufficient for <code>value</code> to be within <code>+/- error</code> from 0.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="target"></param>
		/// <param name="error"></param>
		/// <param name="message"></param>
		static public void AssertWithinLimits(float value, float target, float error, string message)
		{
			if(!(Math.Abs(target) < error && Math.Abs(value) < error))
				if(Math.Abs(value) > Math.Abs(target) * (1 + error) || Math.Abs(value) < Math.Abs(target) * (1 - error) || Math.Sign(value) != Math.Sign(target))
					throw new DebugAssertException(message);
		}

		static public void AssertWithinLimits(Float32Vector value, Float32Vector target, float error, string message)
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

		static public void AssertOperation(bool condition, string message)
		{
			if (!condition)
				throw new InvalidOperationException(message);
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

		public static List<T> FindAll<T>(IEnumerable<T> set, Predicate<T> pred)
		{
			List<T> l = new List<T>();
			foreach (T t in set)
			{
				if (pred(t))
					l.Add(t);
			}

			return l;
		}

		public static List<TReturn> ConvertAll<T, TReturn>(IEnumerable<T> set, Converter<T, TReturn> conv)
		{
			List<TReturn> l = new List<TReturn>();
			foreach (T t in set)
			{
				l.Add(conv(t));
			}

			return l;
		}

		/// <summary>
		/// You can use this one to make resharper think that a variable is used so that it won't
		/// show a warning. Can be handy when the variable isn't used for anything but debugging, 
		/// or when resharpers value analysis is making a mistake.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="var"></param>
		[Conditional("XXXY")]
		public static void PretendVariableIsUsed<T>(T var)
		{
			
		}

		public static int Align4(int value)
		{
			return (value + 3) & ~3;
		}

		public static IntPtr Align16(IntPtr value)
		{
			return (IntPtr) (((long)value + 15) & ~15);
		}

		public static int Align16(int value)
		{
			return (value + 15) & ~15;
		}

		public static long Align16(long value)
		{
			return (value + 15) & ~15;
		}

		public static long Align128(long value)
		{
			return (value + 127) & ~127;
		}

		public static int Align128(int value)
		{
			return (value + 127) & ~127;
		}

		public static bool IsWordAligned(int value)
		{
			return (value%4) == 0;
		}

		public static bool IsWordAligned(IntPtr ptr)
		{
			return IsWordAligned((int)ptr);
		}

		internal static bool IsWordAligned(LocalStorageAddress lsa)
		{
			return IsWordAligned(lsa.Value);
		}

		public static bool IsDoubleWordAligned(int value)
		{
			return (value%8) == 0;
		}

		public static bool IsDoubleWordAligned(IntPtr ptr)
		{
			return IsDoubleWordAligned((int) ptr);
		}

		internal static bool IsDoubleWordAligned(LocalStorageAddress lsa)
		{
			return IsDoubleWordAligned(lsa.Value);
		}

		internal static bool IsQuadwordAligned(LocalStorageAddress lsa)
		{
			return IsQuadwordAligned(lsa.Value);
		}

		public static bool IsQuadwordAligned(int lsa)
		{
			return (lsa % 16) == 0;
		}

		public static bool IsQuadwordAligned(IntPtr ea)
		{
			return ((long)ea % 16) == 0;
		}

		public static bool IsQuadwordMultiplum(int bytecount)
		{
			return (bytecount % 16) == 0;
		}

		static public void CopyCode(int[] src, int srcOffset, int[] dest, int destOffset, int count)
		{
			Buffer.BlockCopy(src, srcOffset*4, dest, destOffset*4, count*4);
		}

		static public byte[] WriteBigEndianBytes(int[] src)
		{
			byte[] buf = new byte[src.Length * 4];
			for (int i = 0; i < src.Length; i++)
			{
				uint e = (uint) src[i];
				int byteidx = i*4;

				buf[byteidx] = (byte) (e >> 24);
				buf[byteidx+1] = (byte) (e >> 16);
				buf[byteidx + 2] = (byte)(e >> 8);
				buf[byteidx + 3] = (byte)(e);
			}

			return buf;
		}

		static internal void DumpMemory(SpeContext context, LocalStorageAddress lsa, int bytecount, TextWriter writer)
		{
			AssertArgument(IsQuadwordAligned(lsa), "IsQuadwordAligned(lsa): " + lsa.Value.ToString("x6"));

			int[] mem = context.GetCopyOfLocalStorage16K();
			if (lsa.Value + bytecount > mem.Length * 4)
				throw new ArgumentException("Memory out of range.");
			DumpMemory(mem, lsa.Value / 4, lsa, bytecount, writer);
		}

		static internal void DumpMemoryToConsole(byte[] memDump)
		{
			DumpMemory(memDump, 0, (LocalStorageAddress) 0, memDump.Length, Console.Out);
		}

		static internal void DumpMemory(byte[] memDump, int arrayOffset, LocalStorageAddress arrayOffsetAddress, int bytecount, TextWriter writer)
		{
			int[] copy = new int[Align4(bytecount)];
			Buffer.BlockCopy(memDump, arrayOffset, copy, 0, bytecount);
			DumpMemory(copy, 0, arrayOffsetAddress, bytecount, writer);
		}

		static internal void DumpMemory(int[] memDump, int arrayOffset, LocalStorageAddress arrayOffsetAddress, int bytecount, TextWriter writer)
		{
			int bytesPerLine = 16;
			for (int i = 0; i < Math.Min(memDump.Length, bytecount / 4); i++)
			{
				int address = arrayOffsetAddress.Value + i*4;
				if (address % bytesPerLine == 0)
				{
					if (i > 0)
						writer.WriteLine();

					writer.Write("{0:x6}: ", address);
				}
				else if (address % 8 == 0)
					writer.Write(" ");
				else if (address % 4 == 0)
					writer.Write(" ");

				uint val = (uint)memDump[arrayOffset + i];
				writer.Write(" {0:x2} {1:x2} {2:x2} {3:x2}", val >> 0x18, (val >> 0x10) & 0xff, (val >> 8) & 0xff, val & 0xff);
			}
			writer.WriteLine();

		}

		public static Set<T> RemoveDuplicates<T>(IEnumerable<T> enumerable)
		{
			Set<T> s = new Set<T>();
			s.AddAll(enumerable);
			return s;
		}

		static internal unsafe uint ReinterpretAsUInt(float value)
		{
			return *((uint*) &value);
		}

		static internal unsafe int ReinterpretAsInt(float value)
		{
			return *((int*)&value);
		}

		internal static unsafe float ReinterpretAsSingle(int value)
		{
			return *(((float*)&value));
		}

		internal static unsafe double ReinterpretAsDouble(long value)
		{
			return *(((double*)&value));
		}
		static internal unsafe long ReinterpretAsLong(double value)
		{
			return *((long*)&value);
		}
	}
}

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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CellDotNet.Spe;
using JetBrains.Annotations;

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

		[AssertionMethod]
		static public void AssertArgument([AssertionCondition(AssertionConditionType.IS_TRUE)] bool condition, string message)
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

		[AssertionMethod]
		static public void AssertNotNull([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] object arg, string expressionOrMessage)
		{
			if (arg == null)
				throw new DebugAssertException("An expression is null: " + expressionOrMessage);
		}

		[AssertionMethod]
		static public void AssertNull([AssertionCondition(AssertionConditionType.IS_NULL)] object arg, string expressionOrMessage)
		{
			if (arg != null)
				throw new DebugAssertException("An expression is not null: " + expressionOrMessage);
		}

		[AssertionMethod]
		static public void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)]bool condition, string message)
		{
			if (!condition)
				throw new DebugAssertException(message);
		}

		[Conditional("DEBUG")]
		[AssertionMethod]
		static public void DebugAssert([AssertionCondition(AssertionConditionType.IS_TRUE)]bool condition, string message)
		{
//			Debug.Assert(condition);
			if (!condition)
				throw new DebugAssertException(message);
		}

		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		static public void DebugAssert(bool condition)
		{
			Debug.Assert(condition);
			if (!condition)
				throw new DebugAssertException();
		}

		static public void AssertOperation(bool condition, string message)
		{
			if (!condition)
				throw new InvalidOperationException(message);
		}

		#endregion

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

			var alignedLsa = (LocalStorageAddress) (lsa.Value & ~0xf);
			byte[] mem = context.GetLocalStorageMax16K(alignedLsa, Align16(bytecount));
			if (lsa.Value + bytecount > mem.Length)
				throw new ArgumentException("Memory out of range.");
			DumpMemory(mem, lsa.Value - alignedLsa.Value, (LocalStorageAddress)lsa.Value, bytecount, writer);
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
			const int bytesPerLine = 16;
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

		public static HashSet<T> RemoveDuplicates<T>(IEnumerable<T> enumerable)
		{
			return new HashSet<T>(enumerable);
		}

		static internal unsafe uint ReinterpretAsUInt(float value)
		{
			return *((uint*) &value);
		}

		static internal unsafe ulong ReinterpretAsULong(double value)
		{
			return *((ulong*) &value);
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

		static public void BigEndianToHost(int[] code)
		{
			if (!BitConverter.IsLittleEndian)
				return;

			for (int i = 0; i < code.Length; i++)
			{
				uint w = (uint) code[i];
				uint w2 = (w >> 24) | ((w >> 8) & 0xff00) | ((w << 8) & 0xff0000) | (w << 24);
				code[i] = (int) w2;
			}
		}

		static public void HostToBigEndian(int[] code)
		{
			if (!BitConverter.IsLittleEndian)
				return;

			for (int i = 0; i < code.Length; i++)
			{
				uint w = (uint) code[i];
				uint w2 = (w >> 24) | ((w >> 8) & 0xff00) | ((w << 8) & 0xff0000) | (w << 24);
				code[i] = (int) w2;
			}
		}
	}
}

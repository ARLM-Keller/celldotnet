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
using System.Runtime.InteropServices;
using System.Text;

namespace CellDotNet.Spe
{
	/// <summary>
	/// This class is used to marshal values into and out of spu format.
	/// </summary>
	class Marshaler
	{
		List<KeyValuePair<int, object>> _objects = new List<KeyValuePair<int, object>>();
		private int _nextObjectKey = 0xf000000;

		public byte[] GetImage(object[] arguments)
		{
			// Allocate some extra room... hackish...
			int currentlyAllocatedQwCount = arguments.Length + 10;

			byte[] argmem = new byte[currentlyAllocatedQwCount * 16];
			int usedQuadWords = 0;
			for (int i = 0; i < arguments.Length; i++)
			{
				object val = arguments[i];
				byte[] buf = null;
				int currentArgQW;

				switch (Type.GetTypeCode(val.GetType()))
				{
					case TypeCode.Double:
						buf = BitConverter.GetBytes((double)val);
						break;
					case TypeCode.UInt16:
						buf = BitConverter.GetBytes((ushort)val);
						break;
					case TypeCode.Int16:
						buf = BitConverter.GetBytes((short)val);
						break;
					case TypeCode.Int32:
						buf = BitConverter.GetBytes((int)val);
						break;
					case TypeCode.UInt32:
						buf = BitConverter.GetBytes((uint)val);
						break;
					case TypeCode.Int64:
						buf = BitConverter.GetBytes((long)val);
						break;
					case TypeCode.Object:
					case TypeCode.String:
						// Handled below.
						break;
					case TypeCode.Single:
						buf = BitConverter.GetBytes((float)val);
						break;
					default:
						throw new NotSupportedException("Unsupported argument datatype: " + val.GetType().Name);
				}

				if (buf == null && !(val is ValueType))
				{
					KeyValuePair<int, object> pair =
						_objects.Find(delegate(KeyValuePair<int, object> obj) { return ReferenceEquals(obj.Value, val); });
					if (pair.Value == null)
					{
						// Allocate new slot.
						pair = new KeyValuePair<int, object>(_nextObjectKey, val);
						_nextObjectKey++;
						_objects.Add(pair);
					}

					buf = BitConverter.GetBytes(pair.Key);
				}

				if (buf != null)
				{
					Buffer.BlockCopy(buf, 0, argmem, usedQuadWords * 16, buf.Length);
					currentArgQW = 1;
				}
				else if (val is ValueType)
				{
					currentArgQW = Utilities.Align16(Marshal.SizeOf(val)) / 16;

					GCHandle h = default(GCHandle);
					try
					{
						h = GCHandle.Alloc(argmem, GCHandleType.Pinned);
						IntPtr argdest = Marshal.UnsafeAddrOfPinnedArrayElement(argmem, usedQuadWords * 16);

						Marshal.StructureToPtr(val, argdest, false);
					}
					finally
					{
						if (h != default(GCHandle))
							h.Free();
					}
				}
				else
				{
					// TODO: Handle reference types.
					throw new NotSupportedException("Unsupported argument datatype: " + val.GetType().Name);
				}

				usedQuadWords += currentArgQW;
			}

			if (currentlyAllocatedQwCount > usedQuadWords)
			{
				byte[] newargmem = new byte[usedQuadWords*16];
				Buffer.BlockCopy(argmem, 0, newargmem, 0, usedQuadWords * 16);
				argmem = newargmem;
			}

			return argmem;
		}

		public object GetValue(byte[] buf, Type type)
		{
			return GetValues(buf, new Type[] {type})[0];
		}

		public object[] GetValues(byte[] buf, Type[] types)
		{
			return GetValues(buf, types, 0);	
		}

		public object[] GetValues(byte[] buf, Type[] types, int offset)
		{
			Utilities.AssertArgument(buf.Length % 16 == 0, "buf.Length % 16 == 0");
			Utilities.AssertArgument(buf.Length >= types.Length * 16, "buf.Length >= types.Length * 16");

			object[] arr = new object[types.Length];

			int currentBufOffset = offset;
			for (int i = 0; i < types.Length; i++)
			{
				Type type = types[i];
				object val = null;

//				Console.WriteLine("getvalues: " + Type.GetTypeCode(type));
//				Console.WriteLine(new StackTrace());
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Single:
						val = BitConverter.ToSingle(buf, currentBufOffset);
						break;
					case TypeCode.Double:
						val = BitConverter.ToDouble(buf, currentBufOffset);
						break;
					case TypeCode.SByte:
						val = (sbyte)buf[currentBufOffset];
						break;
					case TypeCode.Byte:
						val = buf[currentBufOffset];
						break;
					case TypeCode.Int16:
						val = BitConverter.ToInt16(buf, currentBufOffset);
						break;
					case TypeCode.UInt16:
						val = BitConverter.ToUInt16(buf, currentBufOffset);
						break;
					case TypeCode.Int32:
						val = BitConverter.ToInt32(buf, currentBufOffset);
						break;
					case TypeCode.UInt32:
						val = BitConverter.ToUInt32(buf, currentBufOffset);
						break;
					case TypeCode.Int64:
						val = BitConverter.ToInt64(buf, currentBufOffset);
						break;
					case TypeCode.UInt64:
						val = BitConverter.ToUInt64(buf, currentBufOffset);
						break;
					case TypeCode.Object:
					case TypeCode.String:
						// Handled below.
						break;
					default:
						throw new NotSupportedException("Unsupported datatype: " + type.Name);
				}

				int currentValQuadwords;
				if (val != null)
				{
					currentValQuadwords = 1;
				}
				else if (type.IsValueType)
				{
					GCHandle h = default(GCHandle);
					try
					{
						h = GCHandle.Alloc(buf, GCHandleType.Pinned);
						IntPtr src = Marshal.UnsafeAddrOfPinnedArrayElement(buf, currentBufOffset);

						val = Marshal.PtrToStructure(src, type);
					}
					finally
					{
						if (h != default(GCHandle))
							h.Free();
					}

					currentValQuadwords = Utilities.Align16(Marshal.SizeOf(type)) / 16;
				}
				else
				{
					int key = BitConverter.ToInt32(buf, currentBufOffset);
					KeyValuePair<int, object> pair = _objects.Find(delegate(KeyValuePair<int, object> obj) { return obj.Key == key; });
					if (pair.Key == 0)
						throw new ArgumentException("Could not recover reference type '" + type.Name + "' for argument number " + i + ".");

					val = pair.Value;
					currentValQuadwords = 1;
				}

				arr[i] = val;
				currentBufOffset += currentValQuadwords * 16;
			}

			return arr;
		}
	}
}

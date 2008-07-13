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
using System.Runtime.InteropServices;
using CellDotNet.Spe;

namespace CellDotNet
{
	/// <summary>
	/// Represents a pinned array segment which is sutably aligned for MFC DMA.
	/// </summary>
	public class AlignedMemory<T> : IDisposable where T : struct
	{
		private GCHandle _arrayHandle;
		private ArraySegment<T> _arraySegment;

		internal AlignedMemory(GCHandle arrayHandle, ArraySegment<T> arraySegment)
		{
			_arrayHandle = arrayHandle;
			_arraySegment = arraySegment;
		}

		/// <summary>
		/// This one will be deprecated - use <see cref="GetGlobalArea"/> instead.
		/// </summary>
		/// <returns></returns>
		public MainStorageArea GetArea()
		{
			IntPtr ptr = GetIntPtr();
			MainStorageArea area = new MainStorageArea(ptr);

			return area;
		}

		public MainStorageArea GetGlobalArea()
		{
			IntPtr ptr = GetIntPtr();
			MainStorageArea area = new MainStorageArea(ptr);

			return area;
		}

		public IntPtr GetIntPtr()
		{
			if (!_arrayHandle.IsAllocated)
				throw new InvalidOperationException();

			return Marshal.UnsafeAddrOfPinnedArrayElement(_arraySegment.Array, _arraySegment.Offset);
		}

		public ArraySegment<T> ArraySegment
		{
			get { return _arraySegment; }
		}

		public T this[int index]
		{
			get { return _arraySegment.Array[_arraySegment.Offset + index]; }
			set { _arraySegment.Array[_arraySegment.Offset + index] = value; }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool isDisposing)
		{
			if (_arrayHandle.IsAllocated)
			{
				_arrayHandle.Free();
				GC.SuppressFinalize(this);
				_arraySegment = new ArraySegment<T>();
			}
		}

		~AlignedMemory()
		{
			Dispose(false);
		}
	}
}

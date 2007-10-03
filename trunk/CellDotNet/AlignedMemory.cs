using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

		public MainStorageArea GetArea()
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
			if (_arrayHandle.IsAllocated)
			{
				_arrayHandle.Free();
				GC.SuppressFinalize(this);
				_arraySegment = new ArraySegment<T>();
			}
		}

		~AlignedMemory()
		{
			Dispose();
		}
	}
}

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

namespace CellDotNet
{
	/// <summary>
	/// Represent an area in main storage that can be used for dma transfers.
	/// </summary>
	/// <remarks>
	/// This struct will be laid out so that the effective address will be in the preferred slot.
	/// </remarks>
	[Immutable]
	public struct MainStorageArea : IEquatable<MainStorageArea>
	{
		private uint _effectiveAddress;

		internal MainStorageArea(IntPtr effectiveAddress)
		{
			_effectiveAddress = (uint)effectiveAddress;
		}

		internal uint EffectiveAddress
		{
			[IntrinsicMethod(SpuIntrinsicMethod.ReturnArgument1)]
			get { return _effectiveAddress; }
		}

		[IntrinsicMethod(SpuIntrinsicMethod.ReturnArgument1)]
		internal static uint GetEffectiveAddress(MainStorageArea ma)
		{
			return ma._effectiveAddress;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is MainStorageArea)) return false;
			MainStorageArea mainStorageArea = (MainStorageArea) obj;
			return _effectiveAddress == mainStorageArea._effectiveAddress;
		}

		public override int GetHashCode()
		{
			return (int) _effectiveAddress;
		}

		public bool Equals(MainStorageArea other)
		{
			return this == other;
		}

		public static bool operator==(MainStorageArea x, MainStorageArea y)
		{
			return x._effectiveAddress == y._effectiveAddress;
		}

		public static bool operator !=(MainStorageArea x, MainStorageArea y)
		{
			return !(x == y);
		}
	}
}

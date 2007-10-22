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

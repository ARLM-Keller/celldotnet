using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	/// <summary>
	/// Contains static methods for DMA operations by an SPE. 
	/// <para>It closely mirrors the functionality of libspe.</para>
	/// </summary>
	static class Mfc
	{
		/// <summary>
		/// Returns the number of MFC queue entries that currently are unused.
		/// </summary>
		/// <returns></returns>
		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_GetAvailableQueueEntries)]
		static public int GetAvailableQueueEntries()
		{
			throw new InvalidOperationException();
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Put)]
		static unsafe public void Put(void *ls, int ea, int size, uint tag, uint tid, uint rid)
		{
			throw new InvalidOperationException();			
		}

		static public void Get(int[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count*4;

			if (SpuRuntime.IsRunningOnSpu)
			{
				Get(ref target[0], ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
			}
			else
			{
				AssertValidEffectiveAddress(ea.EffectiveAddress, bytecount);
				Marshal.Copy((IntPtr) ea.EffectiveAddress, target, 0, count);
			}
		}

		private static void AssertValidEffectiveAddress(int address, int bytecount)
		{
			if (bytecount % 16 == 0)
				Utilities.Assert(address % 16 == 0, "address % 16 == 0");
			else if (bytecount % 8 == 0)
				Utilities.Assert(address % 8 == 0, "address % 8 == 0");
			else if (bytecount % 4 == 0)
				Utilities.Assert(address % 4 == 0, "address % 4 == 0");
			else if (bytecount % 2 == 0)
				Utilities.Assert(address % 2 == 0, "address % 2 == 0");
			else if (bytecount % 1 == 0)
				Utilities.Assert(address % 1 == 0, "address % 1 == 0");
			else 
				throw new ArgumentOutOfRangeException();
		}

		static public void Put(int[] source, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;

			if (SpuRuntime.IsRunningOnSpu)
			{
				Put(ref source[0], ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
			}
			else
			{
				AssertValidEffectiveAddress(ea.EffectiveAddress, bytecount);
				Marshal.Copy(source, 0, (IntPtr) ea.EffectiveAddress, count);
			}
		}

		[SpuOpCode(SpuOpCodeEnum.Rdch)]
		[return: SpuInstructionPart(SpuInstructionPart.Rt)]
		static private int ReadChannel([SpuInstructionPart(SpuInstructionPart.Ca)]SpuReadChannel channel)
		{
			Utilities.PretendVariableIsUsed(channel);
			throw new InvalidOperationException();
		}

		[SpuOpCode(SpuOpCodeEnum.Wrch)]
		static private void WriteChannel(
			[SpuInstructionPart(SpuInstructionPart.Ca)] SpuWriteChannel channel, 
			[SpuInstructionPart(SpuInstructionPart.Rt)] uint value)
		{
			Utilities.PretendVariableIsUsed(channel);
			Utilities.PretendVariableIsUsed(value);
			throw new InvalidOperationException();
		}

		static public void WaitForDmaCompletion(uint tagMask)
		{
			const uint SPE_TAG_ALL = 1;
//			const uint SPE_TAG_ANY = 2;
//			const uint SPE_TAG_IMMEDIATE = 3;

			if (SpuRuntime.IsRunningOnSpu)
			{
				WriteChannel(SpuWriteChannel.MFC_WrTagMask, tagMask);
				WriteChannel(SpuWriteChannel.MFC_WrTagUpdate, SPE_TAG_ALL);
			}
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Get)]
		static private void Get<T>(ref T lsStart, int ea, int byteCount, uint tag, uint tid, uint rid) where T : struct
		{
			throw new InvalidOperationException();
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Put)]
		static private void Put<T>(ref T lsStart, int ea, int byteCount, uint tag, uint tid, uint rid) where T : struct
		{
			throw new InvalidOperationException();
		}

	}
}

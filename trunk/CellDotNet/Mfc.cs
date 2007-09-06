using System;
using System.Collections.Generic;

namespace CellDotNet
{
	using CellDotNet_SpuOpCode=CellDotNet.SpuOpCode;

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

		static public void Get(int[] target, MainStorageArea ea, int count, uint tag)
		{
			Get(ref target[0], ea.EffectiveAddress, count * 4, 0xfffff, 0, 0);
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

		static public void WaitForCompletion(uint tagMask)
		{
			const uint SPE_TAG_ALL = 1;
			const uint SPE_TAG_ANY = 2;
			const uint SPE_TAG_IMMEDIATE = 3;

			WriteChannel(SpuWriteChannel.MFC_WrTagMask, tagMask);
			WriteChannel(SpuWriteChannel.MFC_WrTagUpdate, SPE_TAG_ALL);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Get)]
		static private void Get<T>(ref T lsStart, int ea, int size, uint tag, uint tid, uint rid)
		{
			throw new InvalidOperationException();
		}
	}
}

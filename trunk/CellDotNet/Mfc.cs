using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	/// <summary>
	/// Contains static methods for DMA operations by an SPE. 
	/// <para>It closely mirrors the functionality of libspe.</para>
	/// Note: The synchronus wrapper methods <code>Get(int[] target, MainStorageArea ea)</code>
	/// and <code>Put(int[] target, MainStorageArea ea)</code> uses taggroup <code>31</code>.
	/// To avoid dedlock do not use tag <code>31</code> while using both the wrapped an
	/// non-wrapped <code>Get</code> and <code>Put</code>.
	/// 
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

		static public void Get(int[] target, MainStorageArea ea)
		{
			Get(target, ea, (short) target.Length, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Get(int[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count*4;

			if (SpuRuntime.IsRunningOnSpu)
			{
				Get(ref target[0], ea.EffectiveAddress, bytecount, 0xfffff, 0, 0); //TODO få styr på tag
			}
			else
			{
				AssertValidEffectiveAddress(ea.EffectiveAddress, bytecount);
				Marshal.Copy((IntPtr)ea.EffectiveAddress, target, 0, count);
			}
		}

		static public void Get_DEBUG(int[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;
			Get(ref target[0], ea.EffectiveAddress, bytecount, tag, 0, 0);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Runtime_Stop)]
		static void Stop()
		{
			
		}


		private static void AssertValidEffectiveAddress(uint address, int bytecount)
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

		static public void Put(int[] target, MainStorageArea ea)
		{
			Put(target, ea, (short)target.Length, 31);
			WaitForDmaCompletion(1);
		}

		static public void Put(int[] source, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;

			if (SpuRuntime.IsRunningOnSpu)
			{
				Put(ref source[0], ea.EffectiveAddress, bytecount, tag, 0, 0);
			}
			else
			{
				AssertValidEffectiveAddress(ea.EffectiveAddress, bytecount);
				Marshal.Copy(source, 0, (IntPtr)ea.EffectiveAddress, count);
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

		[SpuOpCode(SpuOpCodeEnum.Wrch)]
		static private void WriteChannel(
			[SpuInstructionPart(SpuInstructionPart.Ca)] SpuWriteChannel channel,
			[SpuInstructionPart(SpuInstructionPart.Rt)] ref int value)
		{
			Utilities.PretendVariableIsUsed(channel);
			Utilities.PretendVariableIsUsed(value);
			throw new InvalidOperationException();
		}

		static public void WaitForDmaCompletion(uint tagMask)
		{
			const int SPE_TAG_ALL = 2;
//			const int SPE_TAG_ANY = 1;
//			const int SPE_TAG_IMMEDIATE = 3;

			if (SpuRuntime.IsRunningOnSpu)
			{
				WriteChannel(SpuWriteChannel.MFC_WrTagMask, tagMask);
				//TODO skal der ventes på en eller alle DMA'er i den givende tag maske?
				WriteChannel(SpuWriteChannel.MFC_WrTagUpdate, SPE_TAG_ALL);
				int r = ReadChannel(SpuReadChannel.MFC_RdTagStat);
			}
		}

		static private void Get(ref int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{
			if (!SpuRuntime.IsRunningOnSpu)
				throw new InvalidOperationException();

			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint) MfcDmaCommand.Get;
			cmd |= (tid << 24) | (rid << 16);

			WriteChannel(SpuWriteChannel.MFC_LSA, ref lsStart);
			WriteChannel(SpuWriteChannel.MFC_EAL, ea);
			WriteChannel(SpuWriteChannel.MFC_Size, (uint) byteCount);
			WriteChannel(SpuWriteChannel.MFC_TagID, tag & 0x1f);
			WriteChannel(SpuWriteChannel.MFC_CmdAndClassID, cmd);

		}

		static private void Put(ref int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{
			if (!SpuRuntime.IsRunningOnSpu)
				throw new InvalidOperationException();

			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint)MfcDmaCommand.Put;
			cmd |= (tid << 24) | (rid << 16);

			WriteChannel(SpuWriteChannel.MFC_LSA, ref lsStart);
			WriteChannel(SpuWriteChannel.MFC_EAL, ea);
			WriteChannel(SpuWriteChannel.MFC_Size, (uint)byteCount);
			WriteChannel(SpuWriteChannel.MFC_TagID, tag & 0x1f);
			WriteChannel(SpuWriteChannel.MFC_CmdAndClassID, cmd);
		}

		internal enum MfcDmaCommand
		{
			Put = 0x0020,
//			PutS = 0x0028, /*  PU Only */
			PutR = 0x0030,
			PutF = 0x0022,
			PutB = 0x0021,
//			PutFS = 0x002A, /*  PU Only */
//			PutBS = 0x0029, /*  PU Only */
			PutRF = 0x0032,
			PutRB = 0x0031,
			PutL = 0x0024, /* SPU Only */
			PutRL = 0x0034, /* SPU Only */
			PutLF = 0x0026, /* SPU Only */
			PutLB = 0x0025, /* SPU Only */
			PutRLF = 0x0036, /* SPU Only */
			PutRLB = 0x0035, /* SPU Only */

			Get = 0x0040,
//			GetS = 0x0048, /*  PU Only */
			GetF = 0x0042,
			GetB = 0x0041,
//			GetFS = 0x004A, /*  PU Only */
//			GetBS = 0x0049, /*  PU Only */
			GetL = 0x0044, /* SPU Only */
			GetLF = 0x0046, /* SPU Only */
			GetLB = 0x0045, /* SPU Only */
		}
	}
}

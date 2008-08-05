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

namespace CellDotNet.Spe
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
	public static class Mfc
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
			GetLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length*4, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Get(float[] target, MainStorageArea ea)
		{
			GetLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 4, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Get(VectorI4[] target, MainStorageArea ea)
		{
			GetLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 16, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Get(VectorF4[] target, MainStorageArea ea)
		{
			GetLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 16, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Put(int[] target, MainStorageArea ea)
		{
			PutLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 4, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Put(float[] target, MainStorageArea ea)
		{
			PutLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 4, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Put(VectorI4[] target, MainStorageArea ea)
		{
			PutLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 16, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		static public void Put(VectorF4[] target, MainStorageArea ea)
		{
			PutLarge(SpuRuntime.UnsafeGetAddress(target), ea, target.Length * 16, 31);
			WaitForDmaCompletion(uint.MaxValue);
		}

		[CLSCompliant(false)]
		static public void Get(int[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count*4;

				Get(ref target[0], ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
			}

		[CLSCompliant(false)]
		unsafe static public void Get(float[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;

				Get(SpuRuntime.UnsafeGetAddress(target), ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
			}

		/// <summary>
		/// Handels transfers of blocks larger than 16KB.
		/// </summary>
		/// <param name="lsaddress"></param>
		/// <param name="ea"></param>
		/// <param name="bytecount"></param>
		/// <param name="tag"></param>
		[CLSCompliant(false)]
		unsafe static public void GetLarge(int lsaddress, MainStorageArea ea, int bytecount, uint tag)
		{
			uint msa = ea.EffectiveAddress;

			while (bytecount > 0)
			{
				if (GetAvailableQueueEntries() <= 0)
					WaitForAnyDmaCompletion(0xffffffff);

				int blocksize = bytecount > 16 * 1024 ? 16 * 1024 : Utilities.Align16(bytecount);

				Get(lsaddress, msa, blocksize, 0xfffff, 0, 0);

				msa += (uint)blocksize;
				lsaddress += blocksize;
				bytecount -= blocksize;
			}
		}

		/// <summary>
		/// Handels transfere of blocks larger than 16KB.
		/// </summary>
		/// <param name="lsaddress"></param>
		/// <param name="ea"></param>
		/// <param name="bytecount"></param>
		/// <param name="tag"></param>
		[CLSCompliant(false)]
		unsafe static public void PutLarge(int lsaddress, MainStorageArea ea, int bytecount, uint tag)
		{
			uint msa = ea.EffectiveAddress;

			while (bytecount > 0)
			{
				if (GetAvailableQueueEntries() <= 0)
					WaitForAnyDmaCompletion(0xffffffff);

				int blocksize = bytecount > 16 * 1024 ? 16 * 1024 : Utilities.Align16(bytecount);

				Put(lsaddress, msa, blocksize, 0xfffff, 0, 0);

				msa += (uint)blocksize;
				lsaddress += blocksize;
				bytecount -= blocksize;
			}
		}

//		[CLSCompliant(false)]
//		unsafe static public void GetLarge(float[] target, MainStorageArea ea, short count, uint tag)
//		{
//			int bytecount = count * 4;
//
//			int lsaddress = SpuRuntime.UnsafeGetAddress(target);
//
//			if (SpuRuntime.IsRunningOnSpu)
//			{
//				while (bytecount > 0)
//				{
//					if (GetAvailableQueueEntries() <= 0)
//						WaitForAnyDmaCompletion(0xffffffff);
//
//					int blocksize = bytecount > 16 * 1024 ? 16 * 1024 : bytecount;
//
//					Get(lsaddress, ea.EffectiveAddress, blocksize, 0xfffff, 0, 0);
//
//					lsaddress -= blocksize;
//					bytecount -= blocksize;
//				}
//			}
//		}

		[CLSCompliant(false)]
		static public void Get(VectorI4[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 16;

			Get(SpuRuntime.UnsafeGetAddress(target), ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
		}

		[CLSCompliant(false)]
		unsafe static public void Get(VectorF4[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 16;

			Get(SpuRuntime.UnsafeGetAddress(target), ea.EffectiveAddress, bytecount, 0xfffff, 0, 0);
		}

		[CLSCompliant(false)]
		static public void Get_DEBUG(int[] target, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;
			Get(ref target[0], ea.EffectiveAddress, bytecount, tag, 0, 0);
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Runtime_Stop)]
		static void Stop()
		{
			
		}

//		static public void Put(int[] target, MainStorageArea ea)
//		{
//			Put(target, ea, (short)target.Length, 31);
//			WaitForDmaCompletion(1);
//		}

		[CLSCompliant(false)]
		static public void Put(int[] source, MainStorageArea ea, short count, uint tag)
		{
			int bytecount = count * 4;

				Put(ref source[0], ea.EffectiveAddress, bytecount, tag, 0, 0);
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

		[CLSCompliant(false)]
		static public void WaitForDmaCompletion(uint tagMask)
		{
			const int SPE_TAG_ALL = 2;
//			const int SPE_TAG_ANY = 1;
//			const int SPE_TAG_IMMEDIATE = 3;

			WriteChannel(SpuWriteChannel.MFC_WrTagMask, tagMask);
			//TODO skal der ventes på en eller alle DMA'er i den givende tag maske?
			WriteChannel(SpuWriteChannel.MFC_WrTagUpdate, SPE_TAG_ALL);
			int r = ReadChannel(SpuReadChannel.MFC_RdTagStat);
		}

		[CLSCompliant(false)]
		static public void WaitForAnyDmaCompletion(uint tagMask)
		{
			const int SPE_TAG_ANY = 1;
//			const int SPE_TAG_ALL = 2;
//			const int SPE_TAG_IMMEDIATE = 3;

			WriteChannel(SpuWriteChannel.MFC_WrTagMask, tagMask);
			WriteChannel(SpuWriteChannel.MFC_WrTagUpdate, SPE_TAG_ANY);
			int r = ReadChannel(SpuReadChannel.MFC_RdTagStat);
		}

		static private void Get(ref int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{
			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint) MfcDmaCommand.Get;
			cmd |= (tid << 24) | (rid << 16);

			WriteChannel(SpuWriteChannel.MFC_LSA, ref lsStart);
			WriteChannel(SpuWriteChannel.MFC_EAL, ea);
			WriteChannel(SpuWriteChannel.MFC_Size, (uint) byteCount);
			WriteChannel(SpuWriteChannel.MFC_TagID, tag & 0x1f);
			WriteChannel(SpuWriteChannel.MFC_CmdAndClassID, cmd);

		}

		static private void Get(int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{

//			Console.WriteLine(90);

			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint)MfcDmaCommand.Get;
			cmd |= (tid << 24) | (rid << 16);

//			Console.WriteLine(lsStart); //DEBUG
			WriteChannel(SpuWriteChannel.MFC_LSA, (uint)lsStart);
//			Console.WriteLine((int)ea); //DEBUG
			WriteChannel(SpuWriteChannel.MFC_EAL, ea);
//			Console.WriteLine(byteCount); //DEBUG
			WriteChannel(SpuWriteChannel.MFC_Size, (uint)byteCount);
//			Console.WriteLine((int)(tag & 0x1f)); //DEBUG
			WriteChannel(SpuWriteChannel.MFC_TagID, tag & 0x1f);
//			Console.WriteLine((int)cmd); //DEBUG
			WriteChannel(SpuWriteChannel.MFC_CmdAndClassID, cmd);

//			Console.WriteLine(99);
		}

		static private void Put(ref int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{
			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint)MfcDmaCommand.Put;
			cmd |= (tid << 24) | (rid << 16);

			WriteChannel(SpuWriteChannel.MFC_LSA, ref lsStart);
			WriteChannel(SpuWriteChannel.MFC_EAL, ea);
			WriteChannel(SpuWriteChannel.MFC_Size, (uint)byteCount);
			WriteChannel(SpuWriteChannel.MFC_TagID, tag & 0x1f);
			WriteChannel(SpuWriteChannel.MFC_CmdAndClassID, cmd);
		}

		static private void Put(int lsStart, uint ea, int byteCount, uint tag, uint tid, uint rid)
		{
			// MFC_CMD_WORD(_tid, _rid, _cmd) (((_tid)<<24)|((_rid)<<16)|(_cmd))
			uint cmd = (uint)MfcDmaCommand.Put;
			cmd |= (tid << 24) | (rid << 16);

			WriteChannel(SpuWriteChannel.MFC_LSA, (uint)lsStart);
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

using System;
using System.Collections.Generic;

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

//		static unsafe public void Put(void *ls, int ea, int size, tag, tid, rid)
//		{
//			
//		}
//
//		private static unsafe void MfcDma32(void* ls, uint ea, uint size, uint tagid, uint cmd)
//		{
//			si_wrch(MFC_LSA, si_from_ptr(ls));
//			si_wrch(MFC_EAL, si_from_uint(ea));
//			si_wrch(MFC_Size, si_from_uint(size));
//			si_wrch(MFC_TagID, si_from_uint(tagid));
//			si_wrch(MFC_Cmd, si_from_uint(cmd));
//		}


		static int CreateMfcCommand(int _tid, int _rid, int _cmd)
		{
			return (((_tid) << 24) | ((_rid) << 16) | (_cmd));
		}
	}
}

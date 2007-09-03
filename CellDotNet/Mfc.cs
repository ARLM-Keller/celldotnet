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

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Put)]
		static unsafe public void Put(void *ls, int ea, int size, uint tag, uint tid, uint rid)
		{
			throw new InvalidOperationException();			
		}

		[IntrinsicMethod(SpuIntrinsicMethod.Mfc_Get)]
		static unsafe public void Get(void* ls, int ea, int size, uint tag, uint tid, uint rid)
		{
			throw new InvalidOperationException();
		}
	}
}

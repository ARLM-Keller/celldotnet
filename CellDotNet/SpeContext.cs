using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace CellDotNet
{
	internal unsafe class SpeContext : IDisposable
	{
		private const uint SPE_TAG_ALL = 1;
		private const uint SPE_TAG_ANY = 2;
		private const uint SPE_TAG_IMMEDIATE = 3;

		/*
		 * // Doesn't seem to work with mono yet.
		class SpeHandle : CriticalHandle
		{
			public SpeHandle() : base(IntPtr.Zero)
			{
			}

			protected override bool ReleaseHandle()
			{
				return spe_context_destroy(this) == 0;
			}

			public override bool IsInvalid
			{
				get { return handle == IntPtr.Zero; }
			}
		}
		private SpeHandle _handle;
		*/
		private IntPtr _handle;

		private int _localStorageSize = 16*1024;

		public int LocalStorageSize
		{
			get { return _localStorageSize; }
		}

		public SpeContext()
		{
//			Console.WriteLine("SpeContext constructor begin, handler addr={0}", _handle);

			_handle = UnsafeNativeMethods.spe_context_create(0, IntPtr.Zero);
			if (_handle == IntPtr.Zero || _handle == null)
				throw new Exception();

//			_localStorageSize = spe_ls_size_get(_handle);

			Console.WriteLine("localStorageSize: {0}", _localStorageSize);
			
//			Console.WriteLine("SpeContext created, handler addr={0}", _handle);
		}

		public void LoadProgram(int[] code)
		{
			IntPtr dataBufMain = IntPtr.Zero;

			try
			{
				dataBufMain = Marshal.AllocHGlobal(code.Length*4 + 32);

				IntPtr dataBuf = (IntPtr)(((int)dataBufMain + 15) & ~0xf);

				Marshal.Copy(code, 0, dataBuf, code.Length);

				int codeBufSize = ((code.Length*4)/16 + 1)*16;

				uint DMA_tag = 1;
				spe_mfcio_get(0, dataBuf, codeBufSize, DMA_tag, 0, 0);


				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ALL, ref tag_status);

				Console.WriteLine("DMA done.");

				if (waitresult != 0)
					throw new LibSpeException("spe_mfcio_status_tag_read failed.");
			} finally
			{
				if (dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		/// <summary>
		/// Gets a 4-byte integer from the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		public int DmaGetInt32(LocalStorageAddress lsAddress)
		{
			byte* buf = stackalloc byte[31];
			IntPtr ptr = (IntPtr) (((int)buf + 15) & ~15);

			uint DMA_tag = 1;
			spe_mfcio_put(lsAddress.Value, ptr, 16, DMA_tag, 0, 0);

			uint tag_status = 0;
			int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);
			if (waitresult != 0)
				throw new LibSpeException("spe_mfcio_tag_status_read failed.");

			return Marshal.ReadInt32(ptr);
		}

		/// <summary>
		/// Puts an integer to the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		/// <param name="value"></param>
		public void DmaPut(LocalStorageAddress lsAddress, int value)
		{
			uint DMA_tag = 1;
			byte* buf = stackalloc byte[31];
			IntPtr ptr = (IntPtr)(((int)buf + 15) & ~15);

			Marshal.WriteInt32(ptr, value);

			spe_mfcio_get(lsAddress.Value, ptr, 16, DMA_tag, 0, 0);

			uint tag_status = 0;
			int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);
			if (waitresult != 0)
				throw new LibSpeException("spe_mfcio_tag_status_read failed.");
		}

		public int[] GetCopyOffLocalStorage()
		{
			IntPtr dataBufMain = IntPtr.Zero;

			try
			{
				dataBufMain = Marshal.AllocHGlobal(LocalStorageSize + 16);

				IntPtr dataBuf = (IntPtr) (((int) dataBufMain + 15) & ~0xf);

				uint DMA_tag = 2;

				spe_mfcio_put(0, dataBuf, (uint) LocalStorageSize, DMA_tag, 0, 0);

				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

				Console.WriteLine("DMA done.");

				if (waitresult != 0)
					throw new LibSpeException("spe_mfcio_tag_status_read failed.");

				int[] data = new int[LocalStorageSize/4];
				Marshal.Copy(dataBuf, data, 0, LocalStorageSize/4);
				return data;
			}
			finally
			{
				if(dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		public int Run()
		{
			uint entry = 0;
			int rc = UnsafeNativeMethods.spe_context_run(_handle, ref entry, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (rc < 0)
				throw new LibSpeException("spe_context_run failed. Return code:" + rc);
			return rc;
		}

		/// <summary>
		/// Wrapper for the real one which checks the return code.
		/// </summary>
		/// <param name="lsa"></param>
		/// <param name="ea"></param>
		/// <param name="size"></param>
		/// <param name="tag"></param>
		/// <param name="tid"></param>
		/// <param name="rid"></param>
		private void spe_mfcio_get(int lsa, IntPtr ea, int size, uint tag, uint tid, uint rid)
		{
			// Seems like 16 bytes is the smallest transfer unit that works...
			if ((size <= 0) || (size % 16) != 0)
				throw new ArgumentException("Size is not a positive multiplum of 16 bytes.");
			if ((lsa % 16) != 0)
				throw new ArgumentException("Not 16-byte aligned LSA.");
			if (((long)ea % 16) != 0)
				throw new ArgumentException("Not 16-byte aligned EA.");

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_get: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_get(_handle, (uint)lsa, ea, (uint)size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_get failed.");
		}

		/// <summary>
		/// Wrapper for the real one which checks the return code.
		/// </summary>
		/// <param name="lsa"></param>
		/// <param name="ea"></param>
		/// <param name="size"></param>
		/// <param name="tag"></param>
		/// <param name="tid"></param>
		/// <param name="rid"></param>
		private void spe_mfcio_put(int lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid)
		{
			// Seems like 16 bytes is the smallest transfer unit that works...
			if ((size <= 0) || (size % 16) != 0)
				throw new ArgumentException("Size is not a positive multiplum of 16 bytes.");
			if ((lsa % 16) != 0)
				throw new ArgumentException("Not 16-byte aligned LSA.");
			if (((long)ea % 16) != 0)
				throw new ArgumentException("Not 16-byte aligned EA.");

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_put: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_put(_handle, (uint)lsa, ea, size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_put failed.");
		}

		static class UnsafeNativeMethods
		{
			[DllImport("libspe2")]
//		private static extern SpeHandle spe_context_create(uint flags, IntPtr gang);
			public static extern IntPtr spe_context_create(uint flags, IntPtr gang);

			[DllImport("libspe2")]
//		private static extern int spe_context_destroy(SpeHandle handle);
			public static extern int spe_context_destroy(IntPtr handle);

			[DllImport("libspe2")]
//		private static extern IntPtr spe_ls_area_get(SpeHandle spe);
			public static extern IntPtr spe_ls_area_get(IntPtr spe);

			[DllImport("libspe2")]
			public static extern int spe_ls_size_get(IntPtr spe);

			[DllImport("libspe2")]
//		private static extern int spe_context_run(SpeHandle spe, uint* entry, uint runflags, IntPtr argp, IntPtr envp,
//		                                          IntPtr stopinfo);
			public static extern int spe_context_run(IntPtr spe, ref uint entry, uint runflags, IntPtr argp, IntPtr envp,
			                                          IntPtr stopinfo);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_get(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_put(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_tag_status_read(IntPtr spe, uint mask, uint behavior, ref uint tag_status);
		}

		public void Dispose()
		{
/*
			if (!_handle.IsInvalid)
				_handle.Close();
*/
		}
	}
}
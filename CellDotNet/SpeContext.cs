using System;
using System.Collections.Generic;
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
		*/
//		private SpeHandle _handle;
		private IntPtr _handle;

		private int _localStorageSize = 16*1024;

		public int LocalStorageSize
		{
			get { return _localStorageSize; }
		}

		public SpeContext()
		{
//			Console.WriteLine("SpeContext constructor begin, handler addr={0}", _handle);

			_handle = spe_context_create(0, IntPtr.Zero);
			if (_handle == IntPtr.Zero || _handle == null)
				throw new Exception();

//			_localStorageSize = spe_ls_size_get(_handle);

			Console.WriteLine("localStorageSize: {0}", _localStorageSize);
			
//			Console.WriteLine("SpeContext created, handler addr={0}", _handle);
		}

		public bool LoadProgram(int[] code)
		{
			IntPtr dataBufMain = IntPtr.Zero;

			try
			{
				dataBufMain = Marshal.AllocHGlobal(code.Length*4 + 32);

				IntPtr dataBuf = (IntPtr)(((int)dataBufMain + 15) & ~0xf);

				Marshal.Copy(code, 0, dataBuf, code.Length);

				uint codeBufSize = (((uint) code.Length*4)/16 + 1)*16;

				uint DMA_tag = 1;
				Console.WriteLine("Starting DMA.");

				int loadresult = spe_mfcio_get(_handle, 0, dataBuf, codeBufSize, DMA_tag, 0, 0);

				if (loadresult != 0)
				{
					throw new Exception("spe_mfcio_get failed.");
				}

				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ALL, ref tag_status);

				Console.WriteLine("DMA don.");

				if (waitresult != 0)
					throw new Exception("spe_mfcio_status_tag_read failed.");

				return true;

			} finally
			{
				if(dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		public int[] GetCopyOffLocalStorage()
		{
			IntPtr dataBufMain = IntPtr.Zero;

			try
			{
				dataBufMain = Marshal.AllocHGlobal(LocalStorageSize + 16);

				IntPtr dataBuf = (IntPtr) (((int) dataBufMain + 15) & ~0xf);

				uint DMA_tag = 2;
				Console.WriteLine("Starting DMA.");

				int loadresult = spe_mfcio_put(_handle, 0, dataBuf, (uint) LocalStorageSize, DMA_tag, 0, 0);

				if (loadresult != 0)
				{
					throw new Exception("spe_mfcio_put failed.");
				}

				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

				Console.WriteLine("DMA don.");

				if (waitresult == 0)
				{
					int[] data = new int[LocalStorageSize/4];
					Marshal.Copy(dataBuf, data, 0, LocalStorageSize/4);
					return data;
				}
				else
				{
					throw new Exception("spe_mfcio_tag_status_read failed.");
				}
			} finally
			{
				if(dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		public int Run()
		{
			uint entry = 0;
			int rc = spe_context_run(_handle, ref entry, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (rc < 0)
				throw new Exception();
			return rc;
		}

		[DllImport("libspe2.so")]
//		private static extern SpeHandle spe_context_create(uint flags, IntPtr gang);
		private static extern IntPtr spe_context_create(uint flags, IntPtr gang);

		[DllImport("libspe2.so")]
//		private static extern int spe_context_destroy(SpeHandle handle);
		private static extern int spe_context_destroy(IntPtr handle);

		[DllImport("libspe2.so")]
//		private static extern IntPtr spe_ls_area_get(SpeHandle spe);
		private static extern IntPtr spe_ls_area_get(IntPtr spe);

		[DllImport("libspe2.so")]
		private static extern int spe_ls_size_get(IntPtr spe);

		[DllImport("libspe2.so")]
//		private static extern int spe_context_run(SpeHandle spe, uint* entry, uint runflags, IntPtr argp, IntPtr envp,
//		                                          IntPtr stopinfo);
		private static extern int spe_context_run(IntPtr spe, ref uint entry, uint runflags, IntPtr argp, IntPtr envp,
		                                          IntPtr stopinfo);

		[DllImport("libspe2.so")]
		private static extern int spe_mfcio_get(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

		[DllImport("libspe2.so")]
		private static extern int spe_mfcio_put(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

		[DllImport("libspe2.so")]
		private static extern int spe_mfcio_tag_status_read(IntPtr spe, uint mask, uint behavior, ref uint tag_status);

		public void Dispose()
		{
/*
			if (!_handle.IsInvalid)
				_handle.Close();
*/
		}
	}
}
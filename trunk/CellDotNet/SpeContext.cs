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
				Console.WriteLine("Starting DMA.");

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
		public int DmaGetInt32(int lsAddress)
		{
			uint DMA_tag = 1;
//			Align16 block = new Align16();
//			IntPtr ptr = block.Get8BytesAlignedAddress();
			byte* buf = stackalloc byte[7];
			IntPtr ptr = (IntPtr) (((int)buf + 3) & ~3);

			spe_mfcio_put(lsAddress, ptr, 4, DMA_tag, 0, 0);
			return *((int*) ptr);
		}

		/// <summary>
		/// Puts a 4-byte integer to the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		/// <param name="value"></param>
		public void DmaPut(int lsAddress, int value)
		{
			uint DMA_tag = 1;
//			Align16 block = new Align16();
//			IntPtr ptr = block.Get8BytesAlignedAddress();
			byte* buf = stackalloc byte[7];
			IntPtr ptr = (IntPtr)(((int)buf + 3) & ~3);

			spe_mfcio_get(lsAddress, ptr, 4, DMA_tag, 0, 0);
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

				spe_mfcio_put(0, dataBuf, (uint) LocalStorageSize, DMA_tag, 0, 0);

				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

				Console.WriteLine("DMA done.");

				if (waitresult == 0)
				{
					int[] data = new int[LocalStorageSize/4];
					Marshal.Copy(dataBuf, data, 0, LocalStorageSize/4);
					return data;
				}
				else
				{
					throw new LibSpeException("spe_mfcio_tag_status_read failed.");
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
			int loadresult = UnsafeNativeMethods.spe_mfcio_get(_handle, (uint) lsa, ea, (uint) size, tag, tid, rid);

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
			int loadresult = UnsafeNativeMethods.spe_mfcio_put(_handle, (uint) lsa, ea, size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_put failed.");
		}

		static class UnsafeNativeMethods
		{
			[DllImport("libspe2.so")]
//		private static extern SpeHandle spe_context_create(uint flags, IntPtr gang);
			public static extern IntPtr spe_context_create(uint flags, IntPtr gang);

			[DllImport("libspe2.so")]
//		private static extern int spe_context_destroy(SpeHandle handle);
			public static extern int spe_context_destroy(IntPtr handle);

			[DllImport("libspe2.so")]
//		private static extern IntPtr spe_ls_area_get(SpeHandle spe);
			public static extern IntPtr spe_ls_area_get(IntPtr spe);

			[DllImport("libspe2.so")]
			public static extern int spe_ls_size_get(IntPtr spe);

			[DllImport("libspe2.so")]
//		private static extern int spe_context_run(SpeHandle spe, uint* entry, uint runflags, IntPtr argp, IntPtr envp,
//		                                          IntPtr stopinfo);
			public static extern int spe_context_run(IntPtr spe, ref uint entry, uint runflags, IntPtr argp, IntPtr envp,
			                                          IntPtr stopinfo);

			[DllImport("libspe2.so")]
			public static extern int spe_mfcio_get(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2.so")]
			public static extern int spe_mfcio_put(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2.so")]
			public static extern int spe_mfcio_tag_status_read(IntPtr spe, uint mask, uint behavior, ref uint tag_status);
		}

		public void Dispose()
		{
/*
			if (!_handle.IsInvalid)
				_handle.Close();
*/
		}

//		/// <summary>
//		/// A struct that takes up 28 bytes, so it contains a 16 bytes-aligned 16-byte block.
//		/// <para>
//		/// Make sure that the instance can't be relocated/compacted while using the address returned by
//		/// <see cref="Get16BytesAlignedAddress"/> by allocating it on the stack.
//		/// </para>
//		/// </summary>
//		[StructLayout(LayoutKind.Sequential, Pack=16)]
//		public struct Align16
//		{
//			private int i1;
//			private int i2;
//
//			public unsafe IntPtr Get8BytesAlignedAddress()
//			{
//				return Get16BytesAlignedAddress();
//			}
//
//			public unsafe IntPtr Get16BytesAlignedAddress()
//			{
//				fixed (Align16* ptr = &this)
//				{
//					return (IntPtr) ptr;
//				}
//			}
//		}

		/// <summary>
		/// A struct that takes up 28 bytes, so it contains a 16 bytes-aligned 16-byte block.
		/// <para>
		/// Make sure that the instance can't be relocated/compacted while using the address returned by
		/// <see cref="Get16BytesAlignedAddress"/> by allocating it on the stack.
		/// </para>
		/// </summary>
		public struct Align16
		{
			private int i1;
			private int i2;
			private int i3;
			private int i4;
			private int i5;
			private int i6;
			private int i7;

			public unsafe IntPtr Get16BytesAlignedAddress()
			{
				fixed (Align16* ptr = &this)
				{
					long ptr2 = (long)ptr;
					long l = (ptr2 + 16) & ~0xf;

					if (sizeof(void*) == 8)
					{
						// Mono doesn't like this on 32 bit.
						return new IntPtr(l);
					}
					else
					{
						return new IntPtr((int) l);
					}
				}
			}

			public unsafe IntPtr Get8BytesAlignedAddress()
			{
				return Get16BytesAlignedAddress();
			}
		}
	}
}
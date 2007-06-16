using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	internal unsafe class SpeContext : IDisposable
	{
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

		private IntPtr _localStorage;

		public IntPtr LocalStorage
		{
			get { return _localStorage; }
		}

		private int _localStorageSize;

		public int LocalStorageSize
		{
			get { return _localStorageSize; }
		}

		public SpeContext()
		{
			Console.WriteLine("SpeContext onstructor begin, handler addr={0} localstore addr={1}", _handle, _localStorage);

			_handle = spe_context_create(0, IntPtr.Zero);
			if (_handle == IntPtr.Zero || _handle == null)
				throw new Exception();
			_localStorage = spe_ls_area_get(_handle);
			if (_localStorage == null)
				throw new Exception();
			_localStorageSize = spe_ls_size_get(_handle);

			Console.WriteLine("SpeContext created, handler addr={0} localstore addr={1}", _handle, _localStorage);
		}

		public void LoadProgram(int[] code)
		{
			Marshal.Copy(code, 0, _localStorage, code.Length);
		}

		public int[] GetCopyOffLocalStorage()
		{
			int[] data = new int[LocalStorageSize];
			Marshal.Copy(_localStorage, data, 0, LocalStorageSize);
			return data;
		}

		public void Run()
		{
			uint entry;
			int rc = spe_context_run(_handle, &entry, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (rc < 0)
				throw new Exception();
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
		private static extern int spe_context_run(IntPtr spe, uint* entry, uint runflags, IntPtr argp, IntPtr envp,
												  IntPtr stopinfo);

		public void Dispose()
		{
/*
			if (!_handle.IsInvalid)
				_handle.Close();
*/
		}
	}
}
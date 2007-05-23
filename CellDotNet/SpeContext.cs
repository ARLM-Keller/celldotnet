using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	unsafe class SpeContext : IDisposable
	{
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

		private IntPtr _localStorage;
		public IntPtr LocalStorage
		{
			get { return _localStorage; }
		}

		public SpeContext()
		{
			_handle = spe_context_create(0, IntPtr.Zero);
			if (_handle.IsInvalid)
				throw new Exception();
			_localStorage = spe_ls_area_get(_handle);
			if (_localStorage == null)
				throw new Exception();
		}

		public void LoadProgram(int[] code)
		{
			Marshal.Copy(code, 0, _localStorage, code.Length);
		}

		public void Run()
		{
			uint entry;
			int rc = spe_context_run(_handle, &entry, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (rc < 0)
				throw new Exception();
		}

		[DllImport("libspe2")]
		private static extern SpeHandle spe_context_create(uint flags, IntPtr gang);

		[DllImport("libspe2")]
		private static extern int spe_context_destroy(SpeHandle handle);

		[DllImport("libspe2")]
		private static extern IntPtr spe_ls_area_get(SpeHandle spe);

		[DllImport("libspe2")]
		private static extern int spe_context_run(SpeHandle spe, uint* entry, uint runflags, IntPtr argp, IntPtr envp, IntPtr stopinfo);

		public void Dispose()
		{
			if (!_handle.IsInvalid)
				_handle.Close();
		}
	}
}

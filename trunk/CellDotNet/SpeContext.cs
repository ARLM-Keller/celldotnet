using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	/// <summary>
	/// Represents a pinned array segment which is sutably aligned for MFC DMA.
	/// </summary>
	public class AlignedMemory<T> : IDisposable where T : struct
	{
		private GCHandle _arrayHandle;
		private ArraySegment<T> _arraySegment;

		internal AlignedMemory(GCHandle arrayHandle, ArraySegment<T> arraySegment)
		{
			_arrayHandle = arrayHandle;
			_arraySegment = arraySegment;
		}

		public MainStorageArea GetArea()
		{
			IntPtr ptr = GetIntPtr();
			MainStorageArea area = new MainStorageArea(ptr);

			return area;
		}

		public IntPtr GetIntPtr()
		{
			if (!_arrayHandle.IsAllocated)
				throw new InvalidOperationException();

			return Marshal.UnsafeAddrOfPinnedArrayElement(_arraySegment.Array, _arraySegment.Offset);
		}

		public ArraySegment<T> ArraySegment
		{
			get { return _arraySegment; }
		}

		public T this[int index]
		{
			get { return _arraySegment.Array[_arraySegment.Offset + index]; }
			set { _arraySegment.Array[_arraySegment.Offset + index] = value; }
		}

		public void Dispose()
		{
			if (_arrayHandle.IsAllocated)
			{
				_arrayHandle.Free();
				GC.SuppressFinalize(this);
				_arraySegment = new ArraySegment<T>();
			}
		}

		~AlignedMemory()
		{
			Dispose();
		}
	}

	public class SpeContext : IDisposable
	{
		private const uint SPE_TAG_ALL = 1;
		private const uint SPE_TAG_ANY = 2;
		private const uint SPE_TAG_IMMEDIATE = 3;

		static object s_lock = new object();

		private static bool? s_hasSpeHardware;
		public static bool HasSpeHardware
		{
			get
			{
				lock (s_lock)
				{
					if (s_hasSpeHardware == null)
					{
						try
						{
							UnsafeNativeMethods.spe_fake_method();
							throw new Exception("WTF??");
						}
						catch (DllNotFoundException)
						{
							s_hasSpeHardware = false;
						}
						catch (EntryPointNotFoundException)
						{
							s_hasSpeHardware = true;
						}
					}
				}

				return s_hasSpeHardware.Value;
			}
		}

		private IntPtr _handle;

		private int _localStorageSize = -1;
		private IntPtr _localStorageMappedAddress;
		private RegisterSizedObject _debugValueObject;

		public int LocalStorageSize
		{
			get { return _localStorageSize; }
		}

		public IntPtr LocalStorageMappedAddress
		{
			get { return _localStorageMappedAddress; }
		}

		internal RegisterSizedObject DebugValueObject
		{
			set {_debugValueObject = value; }
			get { return _debugValueObject;}
		}

		enum SpeContextCreateFlags
		{
			/// <summary>
			/// Configure the SPU Signal Notification 1 Register to be in "logical OR" mode 
			/// instead of the default "Overwrite" mode. 
			/// See Cell Broadband Engine Architecture, SPU Signal Notification Facility.
			/// </summary>
			SPE_CFG_SIGNOTIFY1_OR = 0x00000010,
			/// <summary>
			/// Configure the SPU Signal Notification 2 Register to be in "logical OR" mode
			///  instead of the default "Overwrite" mode. 
			/// See Cell Broadband Engine Architecture, SPU Signal Notification Facility.
			/// </summary>
			SPE_CFG_SIGNOTIFY2_OR = 0x00000020,
			/// <summary>
			/// Request permission for memory-mapped access to the SPE�s problem state area(s). 
			/// See Cell Broadband Engine Architecture, Problem State Memory-Mapped Registers.
			/// <para>
			/// Unfortunately, settings this with libspe 2.1 makes spe_mfcio_tag_status_read() fail.
			/// </para>
			/// </summary>
			SPE_MAP_PS = 0x00000040,
			/// <summary>
			/// Execute this context on an SPU in the isolation mode. 
			/// The specified SPE program must be correctly formatted for isolated execution.
			/// </summary>
			SPE_ISOLATE = 0x00000080,
			/// <summary>
			/// Enable event handling on this SPE context.
			/// </summary>
			SPE_EVENTS_ENABLE = 0x00001000,
		}


		public SpeContext()
		{
			if (HasSpeHardware)
			{
				// Call this one now to warm up, so that we don't risk having an errno overwritten
				// by suddenly loading the posix assembly.
				GetErrorCode();
			}

			_handle = UnsafeNativeMethods.spe_context_create(0, IntPtr.Zero);
			if (_handle == IntPtr.Zero || _handle == null)
				throw new Exception();

			_localStorageSize = UnsafeNativeMethods.spe_ls_size_get(_handle);
			_localStorageMappedAddress = UnsafeNativeMethods.spe_ls_area_get(_handle);
		}

		public void LoadProgram(int[] code)
		{
			AssertHasNativeContext();

			IntPtr dataBufMain = IntPtr.Zero;
			Utilities.AssertArgumentNotNull(code, "code");
			int codeBufSize = Utilities.Align16(code.Length * 4);
			Utilities.AssertArgument(codeBufSize < LocalStorageSize, "codeBufSize < LocalStorageSize");

			try
			{
				dataBufMain = Marshal.AllocHGlobal(code.Length*4 + 32);

				IntPtr dataBuf = (IntPtr) Utilities.Align16((int)dataBufMain);

				Marshal.Copy(code, 0, dataBuf, code.Length);

				DmaGetLarge(dataBuf, codeBufSize, (LocalStorageAddress) 0);
			} 
			finally
			{
				if (dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		/// <summary>
		/// Transfers the data to LS.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="transferSize"></param>
		/// <param name="lsa"></param>
		private void DmaGetLarge(IntPtr buffer, int transferSize, LocalStorageAddress lsa)
		{
			const int MaxDmaSize = 16 * 1024;
			for (int offset = 0; offset < transferSize; offset += MaxDmaSize)
			{
				uint DMA_tag = 1;
				IntPtr bufptr = (IntPtr)((int)buffer + offset);
				int size = Math.Min(transferSize - offset, MaxDmaSize);
				spe_mfcio_get(lsa.Value + offset, bufptr, size, DMA_tag, 0, 0);

				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal s�ttes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ALL, ref tag_status);
				if (waitresult == -1)
					throw new LibSpeException("spe_mfcio_status_tag_read failed.");

				Console.WriteLine("DMA done.");
			}
		}

		internal object DmaGetValue(StackTypeDescription datatype, LocalStorageAddress lsAddress)
		{
			switch (datatype.CliType)
			{
				case CliType.Int32:
				case CliType.NativeInt:
					return DmaGetValue<int>(lsAddress);
				case CliType.Int64:
					break;
				case CliType.Float32:
					return DmaGetValue<float>(lsAddress);
				case CliType.Float32Vector:
					return DmaGetValue<Float32Vector>(lsAddress);
				case CliType.Int32Vector:
					return DmaGetValue<Int32Vector>(lsAddress);
				case CliType.Float64:
				case CliType.ValueType:
				case CliType.ObjectType:
				case CliType.ManagedPointer:
					throw new NotSupportedException();
			}
			throw new NotSupportedException("Data type is not supported: " + datatype);
		}


		/// <summary>
		/// Gets a value type from the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		internal unsafe T DmaGetValue<T>(LocalStorageAddress lsAddress) where T : struct
		{
			byte* buf = stackalloc byte[31];
			IntPtr ptr = Utilities.Align16((IntPtr) buf);

			uint DMA_tag = 1;
			spe_mfcio_put(lsAddress.Value, ptr, 16, DMA_tag, 0, 0);

			uint tag_status = 0;
			int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);
			if (waitresult != 0)
				throw new LibSpeException("spe_mfcio_tag_status_read failed.");

			switch(Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.Int32:
					T? val;
					val = Marshal.ReadInt32(ptr) as T?;
					return val.Value;
				case TypeCode.Single:
					val = *((float*)ptr) as T?;
					return val.Value;
				default:
					if (typeof(T).Equals(typeof(Int32Vector)) || typeof(T).Equals(typeof(Float32Vector)))
					{
						return (T) Marshal.PtrToStructure(ptr, typeof(T));
					}
					else
					{
						throw new NotSupportedException("Type not handled.");
					}
			}
		}

		/// <summary>
		/// This is not the preferred way to pass arguments; they should be embedded in the code.
		/// </summary>
		/// <param name="lsAddress"></param>
		/// <param name="value"></param>
		internal unsafe void DmaPutValue(LocalStorageAddress lsAddress, ValueType value)
		{
			uint DMA_tag = 1;
			byte* buf = stackalloc byte[31];
			IntPtr ptr = Utilities.Align16((IntPtr)buf);

			switch (Type.GetTypeCode(value.GetType()))
			{
				case TypeCode.Int32:
					*((int*)ptr) = (int)value;
					break;
				case TypeCode.Single:
					float f = (float) value;
					*((float*)ptr) = f;
					break;
				default:
					throw new NotSupportedException("Argument type " + value.GetType().Name + " not supported.");
			}

			spe_mfcio_get(lsAddress.Value, ptr, 16, DMA_tag, 0, 0);

			uint tag_status = 0;
			int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);
			if (waitresult != 0)
				throw new LibSpeException("spe_mfcio_tag_status_read failed.");
		}

		/// <summary>
		/// Puts a value type to the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		/// <param name="value"></param>
		internal void DmaPutValue<T>(LocalStorageAddress lsAddress, T value) where T : struct
		{
			DmaPutValue(lsAddress, (ValueType) value);
		}

		public int[] GetCopyOfLocalStorage16K()
		{
			IntPtr dataBufMain = IntPtr.Zero;

			try
			{
				const int sixteenK = 16*1024;

				dataBufMain = Marshal.AllocHGlobal(sixteenK + 16);

				IntPtr dataBuf = (IntPtr) (((int) dataBufMain + 15) & ~0xf);

				uint DMA_tag = 2;

				spe_mfcio_put(0, dataBuf, sixteenK, DMA_tag, 0, 0);

//				Console.WriteLine("Waiting for DMA to finish.");

				// TODO mask skal s�ttes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

//				Console.WriteLine("DMA done.");

				if (waitresult != 0)
					throw new LibSpeException("spe_mfcio_tag_status_read failed.");

				int[] data = new int[sixteenK/4];
				Marshal.Copy(dataBuf, data, 0, sixteenK/4);
				return data;
			}
			finally
			{
				if (dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		public void Run()
		{
			AssertHasNativeContext();

			uint entry = 0;
			SpeStopInfo stopinfo = new SpeStopInfo();

			int rc = UnsafeNativeMethods.spe_context_run(_handle, ref entry, 0, IntPtr.Zero, IntPtr.Zero, ref stopinfo);
			SpuStopCode stopcode = GetStopcode(rc, stopinfo);

			switch (stopcode)
			{
				case SpuStopCode.None:
				case SpuStopCode.ExitSuccess:
				case SpuStopCode.ExitFailure:
					return;
				case SpuStopCode.OutOfMemory:
					throw new SpeOutOfMemoryException();
				case SpuStopCode.PpeCallFailureTest:
					throw new PpeCallException();
				case SpuStopCode.StackOverflow:
					throw new SpeStackOverflowException();
				case SpuStopCode.DebuggerBreakpoint:
					if (_debugValueObject != null)
					{
						throw new SpeDebugException("Debug breakpoint. Debug value: "
							+ DmaGetValue<int>((LocalStorageAddress)_debugValueObject.Offset));
					}
					else
					{
						throw new SpeDebugException("Debug breakpoint.");
					}
				default:
					throw new SpeExecutionException(
						string.Format("An error occurred during execution. The error code is: {0} (0x{1:x}).", 
						stopinfo.SignalCode, stopinfo.SignalCode));
			}
		}

		private static SpuStopCode GetStopcode(int rc, SpeStopInfo stopinfo)
		{
			SpuStopCode stopcode;
			if (rc < 0)
				throw new LibSpeException("spe_context_run failed. Return code: " + rc);
			else if (rc == 0)
			{
				// May be system error; details are in the stop info.
				stopcode = (SpuStopCode) (stopinfo.SignalCode | 0x2000);
			}
			else
			{
				// Maybe it's a custom stop code.
				stopcode = (SpuStopCode) rc;
			}
			return stopcode;
		}

		public object RunProgram(Delegate delegateToRun, params object[] arguments)
		{
			CompileContext cc = new CompileContext(delegateToRun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			return RunProgram(cc, arguments);
		}

		internal object RunProgram(CompileContext cc, params object[] arguments)
		{
			// Run and load.
			int[] code = cc.GetEmittedCode(arguments);
			LoadProgram(code);

			DebugValueObject = cc.DebugValueObject;
			Run();

			// Get return value.
			object retval = null;
			if (cc.EntryPoint.ReturnType != StackTypeDescription.None)
			{
				retval = DmaGetValue(cc.EntryPoint.ReturnType, cc.ReturnValueAddress);
			}

			return retval;
		}

		/// <summary>
		/// This method can be used to execute a method even on windows. It will attempt to 
		/// perform DMA ops as an SPE would.
		/// </summary>
		/// <param name="cc"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static object UnitTestRunProgram(CompileContext cc, params object[] args)
		{
			// Just to make sure...
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (HasSpeHardware)
			{
				using (SpeContext sc = new SpeContext())
					return sc.RunProgram(cc, args);
			}
			else
			{
				return cc.EntryPointAsMetodCompiler.MethodBase.Invoke(null, args);
			}
		}

		internal unsafe SpeControlArea GetControlArea()
		{
			IntPtr ptr = GetProblemState(SpeProblemArea.SPE_MSSYNC_AREA);
			SpeControlArea* areaptr = (SpeControlArea*) ptr;

			return *areaptr;
		}

		private IntPtr GetProblemState(SpeProblemArea area)
		{
			IntPtr ptr = UnsafeNativeMethods.spe_ps_area_get(_handle, area);
			if (ptr == IntPtr.Zero || (long) ptr == -1L)
				throw new LibSpeException();
			return ptr;
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
			AssertHasNativeContext();

			// Seems like 16 bytes is the smallest transfer unit that works...
			if ((size <= 0) || !Utilities.IsQuadwordMultiplum(size))
				throw new ArgumentException("Size is not a positive multiplum of 16 bytes.");
			if (!Utilities.IsQuadwordAligned(lsa))
				throw new ArgumentException("Not 16-byte aligned LSA.");
			if (!Utilities.IsQuadwordAligned(ea))
				throw new ArgumentException("Not 16-byte aligned EA.");

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_get: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_get(_handle, (uint)lsa, ea, (uint)size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_get failed.");
		}

		private void AssertHasNativeContext()
		{
			if (_handle == IntPtr.Zero)
				throw new InvalidOperationException("No context.");
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
		private void spe_mfcio_put(int lsa, IntPtr ea, int size, uint tag, uint tid, uint rid)
		{
			AssertHasNativeContext();

			// Seems like 16 bytes is the smallest transfer unit that works...
			if ((size <= 0) || !Utilities.IsQuadwordMultiplum(size))
				throw new ArgumentException("Size is not a positive multiplum of 16 bytes.");
			if (!Utilities.IsQuadwordAligned(lsa))
				throw new ArgumentException("Not 16-byte aligned LSA.");
			if (!Utilities.IsQuadwordAligned(ea))
				throw new ArgumentException("Not 16-byte aligned EA.");

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_put: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_put(_handle, (uint)lsa, ea, (uint)size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_put failed.");
		}

		public static AlignedMemory<int> AllocateAlignedInt32(int count)
		{
			return AllocateAlignedFourByteElementArray<int>(count, 4);
		}

		public static AlignedMemory<float> AllocateAlignedFloat(int count)
		{
			return AllocateAlignedFourByteElementArray<float>(count, 4);
		}

		/// <summary>
		/// Returns a pinned and suitably aligned array segment containing the requested number of elements.
		/// </summary>
		/// <param name="count"></param>
		/// <param name="elementSize"></param>
		private static unsafe AlignedMemory<T> AllocateAlignedFourByteElementArray<T>(int count, int elementSize) where T : struct
		{
			// Minimal padding to ensure that a final 16-byte dma write can't damage other data.
			int paddedByteCount = Utilities.Align16(count * elementSize);

			// Allocate a sufficently large array.
			// We only 128-align relatively large arrays.
			bool align128 = paddedByteCount > 64;
			int bytesToAllocate;
			if (align128)
				bytesToAllocate = paddedByteCount + 128;
			else
				bytesToAllocate = paddedByteCount + 16;

			T[] arr = new T[bytesToAllocate / elementSize];

			GCHandle gchandle = GCHandle.Alloc(arr, GCHandleType.Pinned);

			// Find an aligned element.
			long arrayStartAddress = (long) Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
			long alignedAddress;
			if (align128)
				alignedAddress = Utilities.Align128(arrayStartAddress);
			else
				alignedAddress = Utilities.Align16(arrayStartAddress);

			int segmentStartIndex = (int) (alignedAddress - arrayStartAddress) / elementSize;
			ArraySegment<T> segment = new ArraySegment<T>(arr, segmentStartIndex, count);

			return new AlignedMemory<T>(gchandle, segment);
		}

		public void Dispose()
		{
			if (_handle != IntPtr.Zero)
			{
				UnsafeNativeMethods.spe_context_destroy(_handle);
				_handle = IntPtr.Zero;
			}
		}

		#region class UnsafeNativeMethods

		static class UnsafeNativeMethods
		{
			[DllImport("libspe2")]
			public static extern IntPtr spe_context_create(SpeContextCreateFlags flags, IntPtr gang);

			[DllImport("libspe2")]
			public static extern int spe_context_destroy(IntPtr handle);

			[DllImport("libspe2")]
			public static extern IntPtr spe_ls_area_get(IntPtr spe);

			[DllImport("libspe2")]
			public static extern int spe_ls_size_get(IntPtr spe);

			[DllImport("libspe2")]
			public static extern int spe_context_run(IntPtr spe, ref uint entry, uint runflags, IntPtr argp, IntPtr envp,
			                                          ref SpeStopInfo stopinfo);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_get(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_put(IntPtr spe, uint lsa, IntPtr ea, uint size, uint tag, uint tid, uint rid);

			[DllImport("libspe2")]
			public static extern int spe_mfcio_tag_status_read(IntPtr spe, uint mask, uint behavior, ref uint tag_status);

			[DllImport("libspe2")]
			public static extern IntPtr spe_ps_area_get(IntPtr spe, SpeProblemArea area);

			/// <summary>
			/// Simply used to determine whether we have access to SPE hardware.
			/// </summary>
			/// <returns></returns>
			[DllImport("libspe2")]
			public static extern int spe_fake_method();
		}

		#endregion

		#region struct SpeStopInfo

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// The structure is defined by libspe 2.1 as:
		/// <code>
		/// typedef struct spe_stop_info { 
		///		unsigned int stop_reason; 
		///		union { 
		///			int spe_exit_code; 
		///			int spe_signal_code; 
		///			int spe_runtime_error; 
		///			int spe_runtime_exception; 
		///			int spe_runtime_fatal; 
		///			int spe_callback_error; 
		///			void *__reserved_ptr; 
		///			unsigned long long __reserved_u64;
		///		} result; 
		///		int spu_status; 
		/// } spe_stop_info_t;
		/// </code>
		/// <para>Note that some of the fields that are based on <see cref="Result"/>
		/// directly reflects the contents of <see cref="SpuStatus"/>.
		/// </para>
		/// </remarks>
		[StructLayout(LayoutKind.Explicit)]
		struct SpeStopInfo
		{
			[FieldOffset(0)]
			private SpeStopReason StopReason;
			[FieldOffset(4)]
			private long Result;
			[FieldOffset(12)]
			private int SpuStatus;


			public override string ToString()
			{
				return string.Format("stopreason: {0}; result: {1:x8}; status: {2:x8}",
									 StopReason, Result, SpuStatus);
			}

			/// <summary>
			/// Exit code returned by the SPE program in the range 0..255. 
			/// The convention for stop and signal usage by SPE programs is that
			/// 0x2000-0x20FF are exit events. 0x2100-0x21FF are callback events. 
			/// 0x0 is an invalid instruction runtime error. Signal codes 0x0001-0x1FFF 
			/// are user-defined signals. This convention determines the mapping to the 
			/// respective fields in stopinfo.
			/// </summary>
			public int ExitCode
			{
				get
				{
					if (StopReason != SpeStopReason.SPE_EXIT)
						throw new InvalidOperationException();
					return (int)Result;
				}
			}

			/// <summary>
			/// Stop and signal code sent by the SPE program. The lower 14-bit of this field 
			/// contain the signal number. The convention for stop and signal usage by SPE
			///  programs is that 0x2000-0x20FF are exit events. 0x2100-0x21FF are
			///  callback events. 0x0 is an invalid instruction runtime error. 
			/// Signal codes 0x0001-0x1FFF are user-defined signals. 
			/// This convention determines the mapping to the respective fields in stopinfo.
			/// </summary>
			public int SignalCode
			{
				get
				{
					if (StopReason != SpeStopReason.SPE_EXIT && StopReason != SpeStopReason.SPE_STOP_AND_SIGNAL)
						throw new InvalidOperationException();

					return (int) Result;
				}
			}

			public int RuntimeExceptionCode
			{
				get
				{
					if (StopReason != SpeStopReason.SPE_RUNTIME_ERROR)
						throw new InvalidOperationException();
					return (int)Result;
				}
			}

			/// <summary>
			/// Contains the (implementation-dependent) errno as set by the 
			/// underlying system call that failed.
			/// </summary>
			public int RuntimeFatalCode
			{
				get
				{
					if (StopReason != SpeStopReason.SPE_RUNTIME_FATAL)
						throw new InvalidOperationException();
					return (int)Result;
				}
			}

			/// <summary>
			/// Contains the return code from the failed library callback.
			/// </summary>
			public int CallbackErrorCode
			{
				get
				{
					if (StopReason != SpeStopReason.SPE_CALLBACK_ERROR)
						throw new InvalidOperationException();
					return (int)Result;
				}
			}
		}

		#endregion

		#region enum SpeStopReason ad SpeProblemArea

		/// <summary>
		/// Used by <see cref="SpeStopInfo"/> to specify a stop reason.
		/// <para>The documentation is from the libspe documentation.</para>
		/// </summary>
		enum SpeStopReason
		{
			/// <summary>
			/// SPE program terminated calling exit(code) with code in the range 0..255. 
			/// The code is saved in spe_exit_code.
			/// </summary>
			SPE_EXIT = 1,

			/// <summary>
			/// SPE program stopped because SPU ran a stop and signal instruction. 
			/// Further information in field spe_signal_code.
			/// </summary>
			SPE_STOP_AND_SIGNAL = 2,

			/// <summary>
			/// SPE program stopped asynchronously because of an runtime exception (event) 
			/// described in spe_runtime_exception. In this case, spe_status is meaningless 
			/// and is therefore set to -1. 
			/// <para>
			/// Note: (Linux) This error condition can only be caught and reported by 
			/// spe_context_run if the SPE context was created with the flag SPE_EVENTS_ENABLE. 
			/// Otherwise the Linux kernel generates a signal to indicate the runtime error.
			/// </para>
			/// </summary>
			SPE_RUNTIME_ERROR = 3,

			/// <summary>
			/// The documentation does not state what this one is supposed to signal; it
			/// seems like it's  <see cref="SPE_RUNTIME_ERROR"/> that uses the 
			/// <see cref="SpeStopInfo.RuntimeExceptionCode"/> field.
			/// </summary>
			SPE_RUNTIME_EXCEPTION = 4,

			/// <summary>
			/// SPE program stopped for other reasons, usually fatal operating system errors 
			/// such as insufficient resources. Further information in spe_runtime_fatal. 
			/// In this case, spe_status would be meaningless and is therefore set to -1.
			/// </summary>
			SPE_RUNTIME_FATAL = 5,

			/// <summary>
			/// A library callback returned a non-zero exit value, which is provided
			/// in spe_callback_error. spe_status contains the information about the failed
			/// library callback (spe_status &amp; 0x3fff0000 is the stop code which led to the
			/// library callback.)
			/// </summary>
			SPE_CALLBACK_ERROR = 6,
		}


		/// <summary>
		/// Different spe problem areas that can be retrieved if the context is created 
		/// with the SPE_MAP_PS flag.
		/// </summary>
		enum SpeProblemArea
		{
			SPE_MSSYNC_AREA = 0,
			SPE_MFC_COMMAND_AREA = 1,
			SPE_CONTROL_AREA = 2,
			SPE_SIG_NOTIFY_1_AREA = 3,
			SPE_SIG_NOTIFY_2_AREA = 4
		};

		#endregion

		#region errno

		private static ErrorCodeDelegate s_errorCodeGetter;
		private delegate object ErrorCodeDelegate();

		/// <summary>
		/// Used to get the errno that libspe sets.
		/// </summary>
		public static object GetErrorCode()
		{
			if (!HasSpeHardware)
				throw new InvalidOperationException();

			if (s_errorCodeGetter == null)
			{
				//  /usr/lib/mono/gac/Mono.Posix/2.0.0.0__0738eb9f132ed756/Mono.Posix.dll
				Assembly ass = Assembly.LoadFrom("/usr/lib/mono/gac/Mono.Posix/2.0.0.0__0738eb9f132ed756/Mono.Posix.dll");
				Type s_stdlib = ass.GetType("Mono.Unix.Native.Stdlib");
				MethodInfo method = s_stdlib.GetMethod("GetLastError");

				s_errorCodeGetter = (ErrorCodeDelegate)Delegate.CreateDelegate(
							typeof(ErrorCodeDelegate), method);
			}

			return s_errorCodeGetter();
		}

		#endregion

	}

	#region struct SpuStatusRegister

	/// <summary>
	/// Decodes the SPU_Status register as defined in the CBEA.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	struct SpuStatusRegister
	{
		[FieldOffset(0)] private int _value;

		/// <summary>
		/// SPU stop and signal type code.
		/// If the P bit (stop and signal indication) is set to a �1�, this field provides a copy of bits 18 through 31
		/// of the SPU stop-and-signal instruction that resulted in the SPU stop. Bits 0 and 1 of this field always
		/// are zeros.
		/// <para>
		/// If the P bit is not set, data in bits 0 through 15 is not valid.
		/// A stop-and-signal with a dependencies (STOPD) instruction, used for debugging, always sets this
		/// field to x'3FFF'.
		/// </para>
		/// </summary>
		public int StopCode
		{
			get
			{
				throw new NotImplementedException();
				// xTODO: This is not a correct decoding.
//				return _value & 0xff;
			}
		}

		private bool GetBit(int bitnum)
		{
			return new BitVector32(_value)[bitnum];
		}

		public bool IsInIsolationExit
		{
			get { return GetBit(21); }
		}

		public bool IsInIsolationLoad
		{
			get { return GetBit(22); }
		}

		public bool IsInIsolationState
		{
			get { return GetBit(24); }
		}

		public bool InvalidChannelInstruction
		{
			get { return GetBit(25); }
		}

		public bool InvalidInstruction
		{
			get { return GetBit(26); }
		}

		public bool IsSingleStepStop
		{
			get { return GetBit(27); }
		}

		public bool IsWaitingOnBlockedChannel
		{
			get { return GetBit(28); }
		}

		public bool IsHalted
		{
			get { return GetBit(29); }
		}

		public bool IsStoppedAndSignaled
		{
			get { return GetBit(30); }
		}

		public bool IsRunning
		{
			get { return GetBit(31); }
		}
	}

	#endregion

	/// <summary>
	/// The spu control area.
	/// </summary>
	/// <remarks>
	/// libspe 2.1 defines this as 
	/// <code>
	/// typedef struct spe_spu_control_area
	/// {
	/// 	unsigned reserved_0_3[4]; // 0
	/// 	unsigned int SPU_Out_Mbox; // 4
	/// 	unsigned char reserved_8_B[4]; // 8
	/// 	unsigned int SPU_In_Mbox; // 12
	/// 	unsigned char reserved_10_13[4]; // 
	/// 	unsigned int SPU_Mbox_Stat; 
	/// 	unsigned char reserved_18_1B[4]; 
	/// 	unsigned int SPU_RunCntl; 
	/// 	unsigned char reserved_20_23[4]; 
	/// 	unsigned int SPU_Status; 
	/// 	unsigned char reserved_28_33[12]; 
	/// 	unsigned int SPU_NPC;
	/// } spe_spu_control_area_t;
	/// </code>
	/// </remarks>
	[StructLayout(LayoutKind.Explicit)]
	struct SpeControlArea
	{

		[FieldOffset(4)]
		public uint SPU_Out_Mbox;

		[FieldOffset(0xc)]
		public uint SPU_In_Mbox;

		[FieldOffset(0x14)]
		public uint SPU_Mbox_Stat;

		[FieldOffset(0x1c)]
		public uint SPU_RunCntl;

		[FieldOffset(0x24)]
		public SpuStatusRegister SPU_Status;

		/// <summary>
		/// SPU Next Program Counter Register.
		/// <para>The SPU Next Program Counter Register contains the address from which an SPU starts executing.</para>
		/// <para>Access to this register is available in all states.</para>
		/// </summary>
		[FieldOffset(0x34)]
		public uint SPU_NPC;
	};
}
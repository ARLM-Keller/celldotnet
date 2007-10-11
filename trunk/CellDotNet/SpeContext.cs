using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	public class SpeContext : IDisposable
	{
		private const uint SPE_TAG_ALL = 1;
		private const uint SPE_TAG_ANY = 2;
//		private const uint SPE_TAG_IMMEDIATE = 3;

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
							throw new LibSpeException("WTF??");
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
			/// Request permission for memory-mapped access to the SPE’s problem state area(s). 
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
			_handle = UnsafeNativeMethods.spe_context_create(0, IntPtr.Zero);
			if (_handle == IntPtr.Zero)
				throw new LibSpeException();

			_localStorageSize = UnsafeNativeMethods.spe_ls_size_get(_handle);
			_localStorageMappedAddress = UnsafeNativeMethods.spe_ls_area_get(_handle);
		}

		private void AssertHasNativeContext()
		{
			if (_handle == IntPtr.Zero)
				throw new InvalidOperationException("No context.");
		}

		#region Running

		internal void Run()
		{
			Run(null, null);
		}

		private void Run(DataObject ppeCallDataArea, Marshaler marshaler)
		{
			AssertHasNativeContext();

			if (marshaler == null)
				marshaler = new Marshaler();

			uint programCounter = 0;
			bool runAgain;

			do
			{
				SpeStopInfo stopinfo = new SpeStopInfo();
				runAgain = false;

				int rc = UnsafeNativeMethods.spe_context_run(_handle, ref programCounter, 0, IntPtr.Zero, IntPtr.Zero, ref stopinfo);
				SpuStopCode stopcode = GetStopcode(rc, stopinfo);

				switch (stopcode)
				{
					case SpuStopCode.None:
					case SpuStopCode.ExitSuccess:
					case SpuStopCode.ExitFailure:
						break;
					case SpuStopCode.OutOfMemory:
						throw new SpeOutOfMemoryException();
					case SpuStopCode.PpeCall:
						{
							// Clear interrupt bit.
							programCounter &= ~(uint)1;

							byte[] argmem = GetLocalStorageMax16K((LocalStorageAddress)ppeCallDataArea.Offset, ppeCallDataArea.Size);
							byte[] returnmem = PerformPpeCall(argmem, marshaler);
							if (returnmem != null && returnmem.Length > 0)
							{
								PutLocalStorage(returnmem, (LocalStorageAddress) ppeCallDataArea.Offset, returnmem.Length);
							}
							runAgain = true;
						}
						break;
					case SpuStopCode.PpeCallFailureTest:
						throw new PpeCallException();
					case SpuStopCode.StackOverflow:
						throw new SpeStackOverflowException();
					case SpuStopCode.DebuggerBreakpoint:
						if (_debugValueObject != null)
						{
							throw new SpeDebugException("Debug breakpoint. Debug value: "
							                            + DmaGetValue<int>((LocalStorageAddress) _debugValueObject.Offset));
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
			} while (runAgain);
		}

		internal static byte[] PerformPpeCall(byte[] argmem, Marshaler marshaler)
		{
			// Figure out which method to call - the first quadword is a RuntimeMethodHandle
			RuntimeMethodHandle rmh;

			GCHandle gc = new GCHandle();
			try
			{
				gc = GCHandle.Alloc(argmem, GCHandleType.Pinned);
				// If the method handle is bad, this will probably blow up the runtime.
				rmh = (RuntimeMethodHandle) Marshal.PtrToStructure(gc.AddrOfPinnedObject(), typeof (RuntimeMethodHandle));
			}
			finally
			{
				if (gc.IsAllocated)
					gc.Free();
			}

			Utilities.Assert(rmh.Value != IntPtr.Zero, "rmh.Value != IntPtr.Zero");

			MethodBase mb = MethodBase.GetMethodFromHandle(rmh);
			if (mb is ConstructorInfo)
				throw new NotSupportedException("Calling PPE type constructors is currently not supported.");

			MethodInfo methodToCall = (MethodInfo) mb;

			// Construct type list for marshaler.
			ParameterInfo[] parameters = methodToCall.GetParameters();
			List<Type> argTypes = new List<Type>(parameters.Length + 1);
			if (!methodToCall.IsStatic)
				argTypes.Add(methodToCall.DeclaringType);

			foreach (ParameterInfo param in parameters)
			{
				argTypes.Add(param.ParameterType);
			}
			object[] values = marshaler.GetValues(argmem, argTypes.ToArray(), 16);


			// Invoke.
			object instance = null;
			object[] arguments;
			if (!methodToCall.IsStatic)
			{
				instance = values[0];
				Utilities.Assert(methodToCall.DeclaringType.IsAssignableFrom(instance.GetType()),
				                 "Instance type: " + instance.GetType().FullName + ". Method type: " + methodToCall.DeclaringType.FullName);

				arguments = new object[values.Length-1];
				Array.Copy(values, 1, arguments, 0, values.Length - 1);
			}
			else
				arguments = values;

			object retval = methodToCall.Invoke(instance, arguments);
			if (methodToCall.ReturnType != typeof(void))
			{
				byte[] retmem = marshaler.GetImage(new object[] { retval });
				return retmem;
			}

			return null;
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

		internal object RunProgram(CompileContext cc, params object[] arguments)
		{
			// Run and load.
			Marshaler marshaler = new Marshaler();
			int[] code = cc.GetEmittedCode(arguments, marshaler);
			LoadProgram(code);

			DebugValueObject = cc.DebugValueObject;
			Run(cc.PpeCallDataArea, marshaler);

			// Get return value.
			object retval = null;
			if (cc.EntryPoint.ReturnType != StackTypeDescription.None)
			{
				if (cc.EntryPoint.ReturnType.CliType == CliType.ObjectType || 
					cc.EntryPoint.ReturnType.CliType == CliType.ValueType)
				{
					// Hopefully suitable size...
					int retvalsize = 8*16;

					byte[] retmem = GetLocalStorageMax16K(cc.ReturnValueAddress, retvalsize);
					retval = marshaler.GetValue(retmem, cc.EntryPoint.ReturnType.ComplexType.ReflectionType);
				}
				else
					retval = DmaGetValue(cc.EntryPoint.ReturnType, cc.ReturnValueAddress);
			}

			return retval;
		}

		public object RunProgram(Delegate delegateToRun, params object[] arguments)
		{
			CompileContext cc = new CompileContext(delegateToRun.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			return RunProgram(cc, arguments);
		}

		public static object UnitTestRunProgram(Delegate del, params object[] args)
		{
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			return UnitTestRunProgram(cc, args);
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

		#endregion

		#region DMA

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

				PutLocalStorage(dataBuf, (LocalStorageAddress) 0, codeBufSize);
			} 
			finally
			{
				if (dataBufMain != IntPtr.Zero)
					Marshal.FreeHGlobal(dataBufMain);
			}
		}

		internal object DmaGetValue(StackTypeDescription datatype, LocalStorageAddress lsAddress)
		{
			byte[] buff = GetLocalStorageMax16K(lsAddress, 16);
			return new Marshaler().GetValue(buff, StackTypeDescription.GetReflectionType(datatype.CliType));
		}

		/// <summary>
		/// Gets a small value type from the specified local storage address.
		/// </summary>
		/// <param name="lsAddress"></param>
		internal unsafe T DmaGetValue<T>(LocalStorageAddress lsAddress) where T : struct
		{
			byte[] buff = GetLocalStorageMax16K(lsAddress, 16);
			return (T) new Marshaler().GetValue(buff, typeof (T));
		}

		/// <summary>
		/// This is not the preferred way to pass arguments; they should be embedded in the code.
		/// </summary>
		/// <param name="lsAddress"></param>
		/// <param name="value"></param>
		internal void DmaPutValue(LocalStorageAddress lsAddress, ValueType value)
		{
			byte[] mem = new Marshaler().GetImage(new object[] { value });
			PutLocalStorage(mem, lsAddress, mem.Length);
		}

		private unsafe byte[] GetLocalStorageMax16K(LocalStorageAddress lsa, int size)
		{
			ValidateDmaTransfer(null, lsa, size);

			byte* ptr = stackalloc byte[size + 16];
			IntPtr buf = Utilities.Align16((IntPtr) ptr);

			const uint DMA_tag = 2;
			spe_mfcio_put(lsa, buf, size, DMA_tag, 0, 0);

			uint tag_status = 0;
			int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

			if (waitresult != 0)
				throw new LibSpeException("spe_mfcio_tag_status_read failed.");

			byte[] data = new byte[size];
			Marshal.Copy(buf, data, 0, size);
			return data;
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

				spe_mfcio_put((LocalStorageAddress) 0, dataBuf, sixteenK, DMA_tag, 0, 0);

				// TODO mask skal sættes til noget fornuftigt
				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ANY, ref tag_status);

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

		private void PutLocalStorage(byte[] buffer, LocalStorageAddress lsa, int transferSize)
		{
			GCHandle gc = new GCHandle();
			try
			{
				gc = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				PutLocalStorage(gc.AddrOfPinnedObject(), lsa, transferSize);
			}
			finally
			{
				if (gc.IsAllocated)
					gc.Free();
			}
		}

		/// <summary>
		/// Transfers the data to LS. Can handle big transfers and assumes that the buffer is properly aligned.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="transferSize"></param>
		/// <param name="lsa"></param>
		private void PutLocalStorage(IntPtr buffer, LocalStorageAddress lsa, int transferSize)
		{
			const int MaxDmaSize = 16 * 1024;
			for (int offset = 0; offset < transferSize; offset += MaxDmaSize)
			{
				uint DMA_tag = 1;
				IntPtr bufptr = (IntPtr)((int)buffer + offset);
				int size = Math.Min(transferSize - offset, MaxDmaSize);
				spe_mfcio_get(lsa + offset, bufptr, size, DMA_tag, 0, 0);

				uint tag_status = 0;
				int waitresult = UnsafeNativeMethods.spe_mfcio_tag_status_read(_handle, 0, SPE_TAG_ALL, ref tag_status);
				if (waitresult == -1)
					throw new LibSpeException("spe_mfcio_status_tag_read failed.");
			}
		}

		/// <summary>
		/// Wrapper for the real one which validates the arguments and checks the return code.
		/// </summary>
		/// <param name="lsa"></param>
		/// <param name="ea"></param>
		/// <param name="size"></param>
		/// <param name="tag"></param>
		/// <param name="tid"></param>
		/// <param name="rid"></param>
		private void spe_mfcio_get(LocalStorageAddress lsa, IntPtr ea, int size, uint tag, uint tid, uint rid)
		{
			AssertHasNativeContext();

			ValidateDmaTransfer(ea, lsa, size);

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_get: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_get(_handle, (uint)lsa.Value, ea, (uint)size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_get failed.");
		}

		/// <summary>
		/// Wrapper for the real one which validates the arguments and checks the return code.
		/// </summary>
		/// <param name="lsa"></param>
		/// <param name="ea"></param>
		/// <param name="size"></param>
		/// <param name="tag"></param>
		/// <param name="tid"></param>
		/// <param name="rid"></param>
		private void spe_mfcio_put(LocalStorageAddress lsa, IntPtr ea, int size, uint tag, uint tid, uint rid)
		{
			AssertHasNativeContext();

			ValidateDmaTransfer(ea, lsa, size);

			Trace.WriteLine(string.Format("Starting DMA: spe_mfcio_put: EA: {0:x8}, LSA: {1:x6}, size: {2:x6}", (int)ea, lsa, size));
			int loadresult = UnsafeNativeMethods.spe_mfcio_put(_handle, (uint)lsa.Value, ea, (uint)size, tag, tid, rid);

			if (loadresult != 0)
				throw new LibSpeException("spe_mfcio_put failed.");
		}

		/// <summary>
		/// Verifies that the addresses and sizes are acceptable for the hardware for a single DMA transfer.
		/// </summary>
		/// <param name="ea"></param>
		/// <param name="lsa"></param>
		/// <param name="size"></param>
		private static void ValidateDmaTransfer(IntPtr? ea, LocalStorageAddress lsa, int size)
		{
			// Seems like 16 bytes is the smallest transfer unit that works...
			if ((size > 0) && Utilities.IsQuadwordMultiplum(size))
			{
				Utilities.AssertArgument(Utilities.IsQuadwordAligned(lsa), "Not 16-byte aligned LSA.");
				Utilities.AssertArgument(ea == null || Utilities.IsQuadwordAligned(ea.Value), "Not 16-byte aligned EA.");
			}
			else if (size == 8)
			{
				Utilities.AssertArgument(Utilities.IsDoubleWordAligned(lsa), "Not 8-byte aligned LSA.");
				if (ea != null)
					Utilities.AssertArgument((ea.Value.ToInt32() % 16) == lsa % 16, "Different ea and lsa remainders for 8-byte transfer.");
			}
			else if (size == 4)
			{
				Utilities.AssertArgument(Utilities.IsWordAligned(lsa), "Not 4-byte aligned LSA.");
				if (ea != null)
					Utilities.AssertArgument((ea.Value.ToInt32() % 16) == lsa % 16, "Different ea and lsa remainders for 8-byte transfer.");
			}
			else throw new ArgumentException("Bad transfer size and/or alignment.");

			Utilities.AssertArgumentRange(size <= 16*1024, "size", size);
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
		private static AlignedMemory<T> AllocateAlignedFourByteElementArray<T>(int count, int elementSize) where T : struct
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

		#endregion

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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool isDisposing)
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
		/// If the P bit (stop and signal indication) is set to a ‘1’, this field provides a copy of bits 18 through 31
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
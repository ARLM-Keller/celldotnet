using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Represents objects (routines and data) that are of vital importance for execution and
	/// might need to be used by the instruction selector.
	/// </summary>
	class SpecialSpeObjects
	{
		#region Objects

		private RegisterSizedObject _nextAllocationStartObject = new RegisterSizedObject("NextAllocationStart");
		private RegisterSizedObject _allocatableByteCountObject = new RegisterSizedObject("AllocatableByteCount");
		private RegisterSizedObject _stackPointerObject = new RegisterSizedObject("InitialStackPointer");
		private RegisterSizedObject _debugValueObject = new RegisterSizedObject("DebugValue");
		private DataObject _ppeCallDataArea = DataObject.FromQuadWords(15, "PpeCallDataArea");
		//		private RegisterSizedObject _stackSizeObject = new RegisterSizedObject("StackSize");
		private ManualRoutine _stackOverflow;
		private ManualRoutine _outOfMemory;

		private DataObject _doubleCompareDataArea = DataObject.FromQuadWords(4, "DoubleCompareDataArea");

		public SpecialSpeObjects()
		{
			_stackOverflow = new ManualRoutine(true, "StackOverflowHandler");
			_stackOverflow.Writer.BeginNewBasicBlock();
			_stackOverflow.Writer.WriteStop(SpuStopCode.StackOverflow);

			_outOfMemory = new ManualRoutine(true, "OomHandler");
			_outOfMemory.Writer.BeginNewBasicBlock();
			_outOfMemory.Writer.WriteStop(SpuStopCode.OutOfMemory);
		}

		public RegisterSizedObject NextAllocationStartObject
		{
			get { return _nextAllocationStartObject; }
		}

		public RegisterSizedObject AllocatableByteCountObject
		{
			get { return _allocatableByteCountObject; }
		}

		public RegisterSizedObject StackPointerObject
		{
			get { return _stackPointerObject; }
		}

		public RegisterSizedObject DebugValueObject
		{
			get { return _debugValueObject; }
		}

		public DataObject PpeCallDataArea
		{
			get { return _ppeCallDataArea; }
		}

//		public RegisterSizedObject StackSizeObject
//		{
//			get { return _stackSizeObject; }
//		}

		public ManualRoutine StackOverflow
		{
			get { return _stackOverflow; }
		}

		public ManualRoutine OutOfMemory
		{
			get { return _outOfMemory; }
		}

		/// <summary>
		/// Static data used for some DP ops.
		/// </summary>
		public DataObject DoubleCompareDataArea
		{
			get { return _doubleCompareDataArea; }
		}

		/// <summary>
		/// Returns all the objects that require storage.
		/// </summary>
		/// <returns></returns>
		public ObjectWithAddress[] GetAll()
		{
			return new ObjectWithAddress[] {
				NextAllocationStartObject, AllocatableByteCountObject, 
				StackPointerObject,
//				StackSizeObject,
				DebugValueObject,
				StackOverflow, 
				OutOfMemory, 
				PpeCallDataArea,
				DoubleCompareDataArea
			};
		}

		#endregion

		#region Actual values.

		private int _nextAllocationStart = -1;
		public int NextAllocationStart
		{
			get
			{
				if (_nextAllocationStart == -1)
					throw new InvalidOperationException();
				return _nextAllocationStart;
			}
		}

		private int _allocatableByteCount = -1;
		public int AllocatableByteCount
		{
			get
			{
				if (_allocatableByteCount == -1)
					throw new InvalidOperationException();
				return _allocatableByteCount;
			}
		}

		private int _initialStackPointer = -1;
		public int InitialStackPointer
		{
			get
			{
				if (_initialStackPointer == -1)
					throw new InvalidOperationException();
				return _initialStackPointer;
			}
		}

		private int _stackSize = -1;
		public int StackSize
		{
			get
			{
				if (_stackSize == -1)
					throw new InvalidOperationException();
				return _stackSize;
			}
		}

		private int[] _doubleCompareData = unchecked(new int[] {
			0x7fffffff, (int)0xffffffff, 0x7fffffff, (int)0xffffffff,
			0x7ff00000,      0x00000000, 0x7ff00000,      0x00000000,
			0x04050607, (int)0xc0c0c0c0, 0x0c0d0e0f, (int)0xc0c0c0c0,
			0x00010203,      0x00010203, 0x08090a0b,      0x08090a0b});

		public int[] DoubleCompareData()
		{
			return _doubleCompareData;
		} 

		public void SetMemorySettings(int initialStackPointer, int stackSize, int nextAllocationStart, int allocatableByteCount)
		{
			const int MemSize = 256*1024;

			Utilities.AssertArgumentRange(initialStackPointer >= 0 && initialStackPointer < MemSize, "initialStackPointer", initialStackPointer);
			Utilities.AssertArgumentRange(stackSize >= 0 && stackSize < MemSize, "stackSize", stackSize);
			Utilities.AssertArgumentRange(nextAllocationStart > 0 && nextAllocationStart < MemSize,
				"nextAllocationStart", nextAllocationStart);
			Utilities.AssertArgumentRange(allocatableByteCount >= 0 && (nextAllocationStart + allocatableByteCount) < MemSize,
				"allocatableByteCount", allocatableByteCount);
			Utilities.AssertArgument(nextAllocationStart + allocatableByteCount + stackSize <= MemSize,
				"Memory settings exceeds memory size.");

			_initialStackPointer = initialStackPointer;
			_stackSize = stackSize;
			_nextAllocationStart = nextAllocationStart;
			_allocatableByteCount = allocatableByteCount;
		}

		#endregion
	}
}

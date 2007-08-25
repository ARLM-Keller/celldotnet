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

		public RegisterSizedObject NextAllocationStartObject
		{
			get { return _nextAllocationStartObject; }
		}

		public RegisterSizedObject AllocatableByteCountObject
		{
			get { return _allocatableByteCountObject; }
		}

		/// <summary>
		/// Returns all the objects that require storage.
		/// </summary>
		/// <returns></returns>
		public ObjectWithAddress[] GetAll()
		{
			return new ObjectWithAddress[] {NextAllocationStartObject, AllocatableByteCountObject};
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

		public bool IsMemoryAllocationEnabled
		{
			get { return _nextAllocationStart != -1; }
		}

		public void SetMemoryAllocationSettings(int nextAllocationStart, int allocatableByteCount)
		{
			const int MaxMem = 256*1024;

			Utilities.AssertArgumentRange(nextAllocationStart > 0 && nextAllocationStart < MaxMem,
				"nextAllocationStart", nextAllocationStart);
			Utilities.AssertArgumentRange(allocatableByteCount >= 0 && (nextAllocationStart + AllocatableByteCount) < MaxMem,
				"allocatableByteCount", allocatableByteCount);

			_nextAllocationStart = nextAllocationStart;
			_allocatableByteCount = allocatableByteCount;
		}

		#endregion
	}
}

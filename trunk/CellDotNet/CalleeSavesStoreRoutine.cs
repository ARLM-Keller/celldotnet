using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// A support routine used to save caller-saves registers. 
	/// It behaves as described in SPU Application Binary Interface Specification, Version 1.7, 
	/// so it can be used to save registers n - 127 on the stack, for some n >= 80, n &lt= 127 .
	/// <para>
	/// Use <see cref="GetSaveAddress"/> to get a reference to a place you can branch to, or use
	/// the object directly to save all.
	/// </para>
	/// <para>
	/// 20070802: Seems like we'll be handling this store/load by having the instruction selector
	/// move back and forth between physical regs and virtual regs, and then having the register
	/// allocator clean it up a bit.
	/// </para>
	/// </summary>
	class CalleeSavesStoreRoutine : SpuDynamicRoutine
	{
		private SpuInstructionWriter _writer;

		public CalleeSavesStoreRoutine()
		{
			_writer = new SpuInstructionWriter();
			_writer.BeginNewBasicBlock();

			// Store the 48 register immediately below SP with reg 128 at the top.
			for (int i = 80; i <= 127; i++)
			{
				int spOffset = -48 + (i - 80);
				_writer.WriteStqd(HardwareRegister.GetHardwareRegister(i), HardwareRegister.SP, spOffset);
			}
		}

		public override int[] Emit()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// startregnum must be >= 80 and &lt= 127.
		/// </summary>
		/// <param name="startregnum"></param>
		/// <returns></returns>
		public ObjectWithAddress GetSaveAddress(int startregnum)
		{
			if (startregnum < 80 || startregnum > 127)
				throw new ArgumentOutOfRangeException("startregnum", startregnum, "Between 80 and 127");

			return new RoutineOffset(this, startregnum - 80);
		}

		public override int Size
		{
			get { throw new NotSupportedException("This is not an independant object, so it should never be necessary to examine it's size."); }
		}

		#region class RoutineOffset

		class RoutineOffset : ObjectWithAddress
		{
			private CalleeSavesStoreRoutine _parent;
			private int _startInstruction;

			public RoutineOffset(CalleeSavesStoreRoutine parent, int startInstruction)
			{
				_parent = parent;
				_startInstruction = startInstruction;
			}

			public override int Offset
			{
				get
				{
					// This assumes that the routine starts processing registers right away.
					return _parent.Offset + (_startInstruction - 80)*4;
				}
				set { throw new InvalidOperationException("This is not an independant object."); }
			}

			public override int Size
			{
				get { throw new InvalidOperationException(); }
			}
		}

		#endregion

		public override void PerformAddressPatching()
		{
			// This one doesn't need patching.
		}
	}
}
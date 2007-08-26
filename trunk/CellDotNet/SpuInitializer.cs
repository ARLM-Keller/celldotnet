using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Used to generate code to initialize an spu and call the initial method.
	/// </summary>
	class SpuInitializer : SpuRoutine
	{
		private SpuInstructionWriter _writer = new SpuInstructionWriter();
		private bool _isPatched;

		public SpuInitializer(ObjectWithAddress initialMethod, RegisterSizedObject returnValueLocation)
			: this(initialMethod, returnValueLocation, null)
		{ }

		/// <summary>
		/// The argument is of type <see cref="ObjectWithAddress"/> so that testing doesn't
		/// have to supply a real method (a <see cref="MethodCompiler"/>); 
		/// but normally a method is passed.
		/// </summary>
		/// <param name="initialMethod"></param>
		/// <param name="returnValueLocation">The location where the return value should be placed. Null is ok.</param>
		/// <param name="stackSizeObject">The object that will hold the stack size. Null is ok.</param>
		public SpuInitializer(ObjectWithAddress initialMethod, RegisterSizedObject returnValueLocation, 
			RegisterSizedObject stackSizeObject)
			: base("SpuInitializer")
		{
			_writer.BeginNewBasicBlock();

			// Initialize stack pointer to two qwords below ls top.
			_writer.WriteLoadI4(HardwareRegister.SP, (256 * 1024) - 0x20);

			// Initialize second word of SP with the stack size.
			if (stackSizeObject != null)
			{
				VirtualRegister stackSizeReg = HardwareRegister.GetHardwareRegister(77);
				VirtualRegister zeroValReg = HardwareRegister.GetHardwareRegister(75);
				VirtualRegister controlReg = HardwareRegister.GetHardwareRegister(76);

				// Load stack size.
				_writer.WriteLoad(stackSizeReg, stackSizeObject);

				// Merge the stack size into second word of SP.
				_writer.WriteLoadI4(zeroValReg, 0);
				_writer.WriteCwd(controlReg, zeroValReg, 4);
				_writer.WriteShufb(HardwareRegister.SP, stackSizeReg, HardwareRegister.SP, controlReg);
			}

			// Store zero to Back Chain.
			VirtualRegister zeroreg = HardwareRegister.GetHardwareRegister(75);
			_writer.WriteLoadI4(zeroreg, 0);
			_writer.WriteStqd(zeroreg, HardwareRegister.SP, 0);

			// Branch to method and set LR.
			_writer.WriteBranchAndSetLink(SpuOpCode.brsl, initialMethod);

			// At this point the method has returned.

			if (returnValueLocation != null)
			{
				_writer.WriteStqr(HardwareRegister.GetHardwareRegister(3), returnValueLocation);
			}

			_writer.WriteStop();
		}

		public override int Size
		{
			get { return _writer.GetInstructionCount()*4; }
		}

		public override int[] Emit()
		{
			return SpuInstruction.emit(_writer.GetAsList());			
		}

		public override void PerformAddressPatching()
		{
			PerformAddressPatching(_writer.BasicBlocks, null);
			_isPatched = true;
		}


		public override IEnumerable<SpuInstruction> GetInstructions()
		{
			if (!_isPatched)
				throw new InvalidOperationException();

			return _writer.GetAsList();
		}
	}
}

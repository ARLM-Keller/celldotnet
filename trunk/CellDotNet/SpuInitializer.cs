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

//		public SpuInitializer(ObjectWithAddress initialMethod, RegisterSizedObject returnValueLocation)
//			: this(initialMethod, returnValueLocation, null)
//		{ }

		/// <summary>
		/// The argument is of type <see cref="ObjectWithAddress"/> so that testing doesn't
		/// have to supply a real method (a <see cref="MethodCompiler"/>); 
		/// but normally a method is passed.
		/// </summary>
		/// <param name="initialMethod"></param>
		/// <param name="returnValueLocation">The location where the return value should be placed. Null is ok.</param>
		/// <param name="argumentValueLocation"></param>
		/// <param name="argumentcount"></param>
		/// <param name="stackPointerObject">The object that will hold the stack pointer.</param>
		/// <param name="NextAllocationStartObject"></param>
		/// <param name="AllocatableByteCountObject"></param>
		public SpuInitializer(ObjectWithAddress initialMethod, RegisterSizedObject returnValueLocation, 
			ObjectWithAddress argumentValueLocation, int argumentcount,
			RegisterSizedObject stackPointerObject,
			RegisterSizedObject NextAllocationStartObject, RegisterSizedObject AllocatableByteCountObject
			) : base("SpuInitializer")
		{
			Utilities.AssertNotNull(stackPointerObject, "stackPointerObject");

			_writer.BeginNewBasicBlock();

			// Patch availabel memory.
			if (NextAllocationStartObject != null && AllocatableByteCountObject != null)
			{
				VirtualRegister r75 = HardwareRegister.GetHardwareRegister(75);
				VirtualRegister r76 = HardwareRegister.GetHardwareRegister(76);

				_writer.WriteBrsl(r75, 1);
				_writer.WriteAi(r75, r75, -4);

				_writer.WriteLoad(r76, NextAllocationStartObject);
				_writer.WriteA(r76, r75, r76);
				_writer.WriteStore(r76, NextAllocationStartObject);

				_writer.WriteLoad(r76, AllocatableByteCountObject);
				_writer.WriteSf(r76, r75, r76);
				_writer.WriteStore(r76, AllocatableByteCountObject);
			}

//			// Initialize second word of SP with the stack size.
//			VirtualRegister stackSizeReg = HardwareRegister.GetHardwareRegister(75);

			// Load stack pointer(including stacksize).
			_writer.WriteLoad(HardwareRegister.SP, stackPointerObject);

//			// Load stack size.
//			_writer.WriteLoad(stackSizeReg, stackSizeObject);

//			// Merge the stack size into second word of SP.
//			_writer.WriteOr(HardwareRegister.SP, HardwareRegister.SP, stackSizeReg);

			// Store zero to Back Chain.
			VirtualRegister zeroreg = HardwareRegister.GetHardwareRegister(75);
			_writer.WriteLoadI4(zeroreg, 0);
			_writer.WriteStqd(zeroreg, HardwareRegister.SP, 0);

			for (int i = 0; i < argumentcount; i++)
				_writer.WriteLoad(HardwareRegister.GetVirtualHardwareRegister((CellRegister)i+3), new ObjectOffset(argumentValueLocation, i*16));

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

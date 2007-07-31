using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Used to generate code to initialize a spu and call the initial method.
	/// </summary>
	class SpuInitializer : SpuRoutine
	{
		private SpuInstructionWriter _writer = new SpuInstructionWriter();

		/// <summary>
		/// The argument is of type <see cref="ObjectWithAddress"/> so that testing doesn't
		/// have to supply a real method; but normally a method is passed.
		/// </summary>
		/// <param name="intialMethod"></param>
		public SpuInitializer(ObjectWithAddress intialMethod)
		{
			_writer.BeginNewBasicBlock();

			// Initialize stack pointer to two qwords below ls top.
			_writer.WriteLoadI4(SpuAbiUtilities.SP, 0x4ffff - 0x20);

			// Store zero to Back Chain.
			VirtualRegister zeroreg = SpuAbiUtilities.GetHardwareRegister(75);
			_writer.WriteLoadI4(zeroreg, 0);
			_writer.WriteStqd(zeroreg, SpuAbiUtilities.SP, 0);

			// Branch to method and set LR.
			// The methode is assumed to be immediately after this code.
			_writer.WriteBrsl(SpuAbiUtilities.LR, 1);
			_writer.WriteBranchAndSetLink(SpuOpCode.brsl, intialMethod);

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
	}
}

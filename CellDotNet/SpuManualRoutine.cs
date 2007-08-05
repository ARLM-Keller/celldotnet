using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Represents code which is not the result of a method compilation, but
	/// rather generated "manually".
	/// <para>
	/// Is this one needed? Currently (20070731) it seems we'll be subclassing
	/// <see cref="ObjectWithAddress"/> to create specialized classes...
	/// </para>
	/// </summary>
	class SpuManualRoutine : SpuRoutine
	{
		public SpuManualRoutine()
		{
			_writer = new SpuInstructionWriter();
		}

		private SpuInstructionWriter _writer;
		public SpuInstructionWriter Writer
		{
			get { return _writer; }
		}

		public override int Size
		{
			get { return Writer.GetInstructionCount() * 4; }
		}

		public override int[] Emit()
		{
			int[] bodybin = SpuInstruction.emit(Writer.GetAsList());
			return bodybin;
		}

		public override void PerformAddressPatching()
		{
			PerformAddressPatching(Writer.BasicBlocks, null);
		}
	}
}

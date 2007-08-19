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
		private bool _omitEpilog = false;

		public SpuManualRoutine(bool omitEpilog)
		{
			_writer = new SpuInstructionWriter();
			_omitEpilog = omitEpilog;
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
			if (_omitEpilog)
				PerformAddressPatching(Writer.BasicBlocks, null);
			else
			{
				SpuAbiUtilities.WriteEpilog(Writer);
				PerformAddressPatching(Writer.BasicBlocks, Writer.CurrentBlock);
			}
		}
	}
}
